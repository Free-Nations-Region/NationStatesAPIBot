using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Interfaces;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Text;
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
        public DateTime LastTelegramSending { get => lastTelegramSending; set => lastTelegramSending = value; }


        public DateTime LastAutomaticNewNationsRequest { get => lastAutomaticNewNationsRequest; set => lastAutomaticNewNationsRequest = value; }
        public DateTime LastAutomaticRegionNationsRequest { get => lastAutomaticRegionNationsRequest; set => lastAutomaticRegionNationsRequest = value; }

        private Task<bool> IsNationStatesApiActionReadyAsync(NationStatesApiRequestType type, bool isScheduledAction)
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
            else
            {
                _logger.LogWarning($"Unrecognized ApiRequestType '{type.ToString()}'");
                return Task.FromResult(false);
            }
        }
        public async Task WaitForAction(NationStatesApiRequestType requestType)
        {
            while (!await IsNationStatesApiActionReadyAsync(requestType, false))
            {
                await Task.Delay((int)TimeSpan.FromTicks(API_REQUEST_INTERVAL).TotalMilliseconds);
            }
        }

        public async Task<XmlDocument> WouldReceiveTelegramAsync(string nationName)
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

        public Task<bool> SendRecruitmentTelegramAsync(string name)
        {
            throw new NotImplementedException();
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
