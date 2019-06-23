using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Manager;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace NationStatesAPIBot.Services
{
    public class NationStatesApiService : BaseApiService
    {
        public const int API_VERSION = 9;
        public const long API_REQUEST_INTERVAL = 6000000; //0,6 s
        public const long SEND_NON_RECRUITMENTTELEGRAM_INTERVAL = 300000000; //30 s
        public const long SEND_RECRUITMENTTELEGRAM_INTERVAL = 1800000000; //3 m 1800000000
        public const long REQUEST_NEW_NATIONS_INTERVAL = 18000000000; //30 m 36000000000
        public const long REQUEST_REGION_NATIONS_INTERVAL = 432000000000; //12 h 432000000000

        public NationStatesApiService(IOptions<AppSettings> config, ILogger<NationStatesApiService> logger) : base(config, logger) { }

        public DateTime LastAPIRequest { get => lastAPIRequest; private set => lastAPIRequest = value; }
        public DateTime LastTelegramSending { get => lastTelegramSending; private set => lastTelegramSending = value; }


        public DateTime LastAutomaticNewNationsRequest { get => lastAutomaticNewNationsRequest; private set => lastAutomaticNewNationsRequest = value; }
        public DateTime LastAutomaticRegionNationsRequest { get => lastAutomaticRegionNationsRequest; private set => lastAutomaticRegionNationsRequest = value; }

        public Task<bool> IsNationStatesApiActionReadyAsync(NationStatesApiRequestType type, bool isScheduledAction)
        {
            if (type == NationStatesApiRequestType.GetNationsFromRegion)
            {
                return Task.FromResult(DateTime.UtcNow.Ticks - LastAutomaticRegionNationsRequest.Ticks > (isScheduledAction ? REQUEST_REGION_NATIONS_INTERVAL : API_REQUEST_INTERVAL));
            }
            else if (type == NationStatesApiRequestType.GetNewNations)
            {
                return Task.FromResult(DateTime.UtcNow.Ticks - LastAutomaticNewNationsRequest.Ticks > (isScheduledAction ? REQUEST_NEW_NATIONS_INTERVAL : API_REQUEST_INTERVAL));
            }
            else if (type == NationStatesApiRequestType.SendRecruitmentTelegram)
            {
                return Task.FromResult(DateTime.UtcNow.Ticks - LastTelegramSending.Ticks > SEND_RECRUITMENTTELEGRAM_INTERVAL);
            }
            else if (type == NationStatesApiRequestType.GetNationStats || type == NationStatesApiRequestType.GetRegionStats || type == NationStatesApiRequestType.WouldReceiveRecruitmentTelegram)
            {
                return Task.FromResult(DateTime.UtcNow.Ticks - LastAPIRequest.Ticks > API_REQUEST_INTERVAL);
            }
            else if (type == NationStatesApiRequestType.DownloadDumps)
            {
                /*
                 * Dump update time according to documentation around 22:30 PDT 
                 * source: https://www.nationstates.net/pages/api#dumps
                 * Add some tolerance of 30 Minutes to it, if it maybe takes longer sometimes
                 * And have a time window of 31 Minutes to it gets definitely hit by the 30 Minute interval of WaitForAction
                 * These times converted to UTC are 6:00 AM and 6:31 AM + 1 hour for possibly daylight saving time issues
                 */
                return Task.FromResult(DateTime.UtcNow.TimeOfDay > new TimeSpan(6, 59, 59) && DateTime.UtcNow.TimeOfDay < new TimeSpan(7, 31, 00));
            }
            else
            {
                _logger.LogCritical($"Unrecognized ApiRequestType '{type.ToString()}'");
                return Task.FromResult(false);
            }
        }
        public async Task WaitForAction(NationStatesApiRequestType requestType)
        {
            await WaitForAction(requestType, TimeSpan.FromTicks(API_REQUEST_INTERVAL));
        }

        public async Task WaitForAction(NationStatesApiRequestType requestType, TimeSpan interval)
        {
            while (!await IsNationStatesApiActionReadyAsync(requestType, true))
            {
                await Task.Delay((int)interval.TotalMilliseconds);
            }
        }

        public async Task WaitForAction(NationStatesApiRequestType requestType, TimeSpan interval, CancellationToken cancellationToken)
        {
            while (!await IsNationStatesApiActionReadyAsync(requestType, true))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                await Task.Delay((int)interval.TotalMilliseconds, cancellationToken);
            }
        }

        public async Task<XmlDocument> GetWouldReceiveTelegramAsync(string nationName)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.WouldReceiveTelegram);
            _logger.LogDebug(id, LogMessageBuilder.Build(id, $"Waiting for WouldReceiveTelegram-Request: {nationName}"));
            await WaitForAction(NationStatesApiRequestType.WouldReceiveRecruitmentTelegram);
            var url = BuildApiRequestUrl($"nation={ToID(nationName)}&q=tgcanrecruit&from={ToID(_config.NationStatesRegionName)}");
            return await ExecuteRequestWithXmlResult(url, id);
        }

        public async Task<XmlDocument> GetRegionStatsAsync(string regionName, EventId eventId)
        {
            _logger.LogDebug(eventId, LogMessageBuilder.Build(eventId, $"Waiting for RegionStats-Request: {regionName}"));
            await WaitForAction(NationStatesApiRequestType.GetRegionStats);
            var url = BuildApiRequestUrl($"region={ToID(regionName)}&q=name+numnations+founded+power+founder+delegate+flag+tags");
            return await ExecuteRequestWithXmlResult(url, eventId);
        }

        public async Task<XmlDocument> GetNationStatsAsync(string nationName, EventId eventId)
        {
            _logger.LogDebug(eventId, LogMessageBuilder.Build(eventId, $"Waiting for NationStats-Request: {nationName}"));
            await WaitForAction(NationStatesApiRequestType.GetNationStats);
            var url = BuildApiRequestUrl($"nation={ToID(nationName)}&q=flag+wa+gavote+scvote+fullname+freedom+demonym2plural+category+population+region+founded+influence+lastactivity+census;mode=score;scale=0+1+2+65+66+80");
            return await ExecuteRequestWithXmlResult(url, eventId);
        }

        public async Task<bool> SendRecruitmentTelegramAsync(string nationName)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.SendRecruitmentTelegram);
            var lastSend = NationManager.GetNationsByStatusName("send").Take(1).ToArray();
            if (lastSend.Length > 0 && !(lastTelegramSending.Year < DateTime.UtcNow.Year))
            {
                lastTelegramSending = lastSend[0].StatusTime;
            }
            try
            {
                _logger.LogDebug(id, LogMessageBuilder.Build(id, $"Waiting for SendRecruitmentTelegram-Request: {nationName}"));
                await WaitForAction(NationStatesApiRequestType.SendRecruitmentTelegram);
                _logger.LogDebug(id, LogMessageBuilder.Build(id, $"Sending Telegram to {nationName}."));
                var url = BuildApiRequestUrl($"a=sendTG" +
                    $"&client={_config.ClientKey}" +
                    $"&tgid={_config.TelegramId}" +
                    $"&key={_config.TelegramSecretKey}" +
                    $"&to={ToID(nationName)}");
                var response = await ExecuteGetRequest(url, id);
                lastTelegramSending = DateTime.UtcNow;
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(id, LogMessageBuilder.Build(id, $"SendRecruitmentTelegram failed with StatusCode {(int)response.StatusCode}: {response.ReasonPhrase}"));
                }
                return response.IsSuccessStatusCode;

            }
            catch (Exception ex)
            {
                _logger.LogError(id, ex, LogMessageBuilder.Build(id, $"An error occured."));
                return false;
            }
        }

        public async Task<List<string>> GetNewNationsAsync(EventId eventId)
        {
            _logger.LogDebug(eventId, LogMessageBuilder.Build(eventId, $"Waiting for GetNewNations-Request"));
            await WaitForAction(NationStatesApiRequestType.GetNewNations);
            var url = BuildApiRequestUrl("q=newnations");
            XmlDocument newNationsXML = await ExecuteRequestWithXmlResult(url, eventId);
            lastAutomaticNewNationsRequest = DateTime.UtcNow;
            XmlNodeList newNationsXMLNodes = newNationsXML.GetElementsByTagName("NEWNATIONS");
            return newNationsXMLNodes[0].InnerText.Split(',').ToList().Select(nation => ToID(nation)).ToList();
        }

        public async Task<XmlDocument> GetFullNationNameAsync(string nationName, EventId eventId)
        {
            _logger.LogDebug(eventId, LogMessageBuilder.Build(eventId, $"Waiting for NationStats(FullName)-Request: {nationName}"));
            await WaitForAction(NationStatesApiRequestType.GetNationStats);
            var url = BuildApiRequestUrl($"nation={ToID(nationName)}&q=fullname");
            return await ExecuteRequestWithXmlResult(url, eventId);
        }

        public async Task<XmlDocument> GetDelegateString(string nationName, EventId eventId)
        {
            _logger.LogDebug(eventId, LogMessageBuilder.Build(eventId, $"Waiting for NationStats(FullName)-Request: {nationName}"));
            await WaitForAction(NationStatesApiRequestType.GetNationStats);
            var url = BuildApiRequestUrl($"nation={ToID(nationName)}&q=fullname+influence+census;mode=score;scale=65+66");
            return await ExecuteRequestWithXmlResult(url, eventId);
        }

        public async Task<XmlDocument> GetEndorsements(string nationName, EventId eventId)
        {
            _logger.LogDebug(eventId, LogMessageBuilder.Build(eventId, $"Waiting for GetEndorsements-Request: {nationName}"));
            await WaitForAction(NationStatesApiRequestType.GetNationStats);
            var url = BuildApiRequestUrl($"nation={ToID(nationName)}&q=endorsements");
            return await ExecuteRequestWithXmlResult(url, eventId);
        }
    }
}
