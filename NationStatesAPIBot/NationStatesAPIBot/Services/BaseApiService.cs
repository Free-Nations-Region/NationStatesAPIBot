using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Types;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace NationStatesAPIBot.Services
{
    public class BaseApiService
    {
        protected readonly ILogger<BaseApiService> _logger;
        protected readonly AppSettings _config;
        protected DateTime lastAPIRequest;
        protected DateTime lastTelegramSending;
        protected DateTime lastAutomaticNewNationsRequest;
        protected DateTime lastAutomaticRegionNationsRequest;
        public BaseApiService(IOptions<AppSettings> config, ILogger<BaseApiService> logger)
        {
            _logger = logger;
            _config = config.Value;
        }
        protected async Task<HttpResponseMessage> ExecuteGetRequest(string url, EventId? eventId)
        {
            bool releaseId = false;
            var logId = eventId != null ? (EventId)eventId : LogEventIdProvider.GetRandomLogEventId();
            lastAPIRequest = DateTime.Now;
            if (eventId == null)
            {
                releaseId = true;
            }
            try
            {
                using (var client = new HttpClient())
                {
                    _logger.LogDebug(logId, LogMessageBuilder.Build(logId, $"Executing Request to {url}"));
                    client.DefaultRequestHeaders.Add("User-Agent", $"NationStatesApiBot/{AppSettings.VERSION}");
                    client.DefaultRequestHeaders.Add("User-Agent", $"(contact { _config.Contact};)");
                    var response = await client.GetAsync(url);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError(logId, LogMessageBuilder.Build(logId, $"Request finished with response: {(int)response.StatusCode}: {response.ReasonPhrase}"));
                    }
                    else
                    {
                        _logger.LogDebug(logId, LogMessageBuilder.Build(logId, $"Request finished with response: {(int)response.StatusCode}: {response.ReasonPhrase}"));
                    }
                    if ((int)response.StatusCode == 429)
                    {
                        _logger.LogDebug(logId, LogMessageBuilder.Build(logId, $"Retry in {response.Headers.RetryAfter.Delta} seconds."));
                    }
                    return response;
                }
            }
            finally
            {
                if (releaseId)
                {
                    LogEventIdProvider.ReleaseEventId(logId);
                }
            }
        }

        protected async Task<Stream> ExecuteRequestWithStreamResult(string url, EventId? eventId)
        {
            var response = await ExecuteGetRequest(url, eventId);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            else
            {
                return null;
            }
        }

        protected async Task<XmlDocument> ExecuteRequestWithXmlResult(string url, EventId eventId)
        {
            using (var stream = await ExecuteRequestWithStreamResult(url, eventId))
            {
                try
                {
                    if (stream != null)
                    {
                        XmlDocument xml = new XmlDocument();
                        xml.Load(stream);
                        return xml;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(eventId, ex, LogMessageBuilder.Build(eventId, $"Some critical error with xml occured.{Environment.NewLine}XML: {await new StreamReader(stream).ReadToEndAsync()}"));
                    return null;
                }
            }
        }

        protected string BuildApiRequestUrl(string parameters)
        {
            return $"http://www.nationstates.net/cgi-bin/api.cgi?{parameters}&v={AppSettings.API_VERSION}";
        }

        public async Task<GZipStream> GetNationStatesDumpStream(NationStatesDumpType type)
        {
            var eventId = LogEventIdProvider.GetRandomLogEventId();
            try
            {

                string url = "https://www.nationstates.net/pages/";
                if (type == NationStatesDumpType.Nations)
                {
                    url += "nations.xml.gz";
                    _logger.LogInformation(eventId, LogMessageBuilder.Build(eventId, "Retrieval of latest Nation dump requested"));
                }
                else if (type == NationStatesDumpType.Regions)
                {
                    url += "regions.xml.gz";
                    _logger.LogInformation(eventId, LogMessageBuilder.Build(eventId, "Retrieval of latest Region dump requested"));
                }
                else
                {
                    throw new NotImplementedException($"Retrieval for DumpType {type} not implemented yet.");
                }
                var stream = await ExecuteRequestWithStreamResult(url, eventId);
                var compressed = new GZipStream(stream,CompressionMode.Decompress);
                return compressed;
                
            }
            finally
            {
                LogEventIdProvider.ReleaseEventId(eventId);
            }
        }

        /// <summary>
        /// Converts nation/region name to format that can be used on api calls
        /// </summary>
        /// <param name="text">The text to ensure format on</param>
        /// <returns>Formated string</returns>
        internal static string ToID(string text)
        {
            return text?.Trim().ToLower().Replace(' ', '_').Trim('@');
        }

        /// <summary>
        /// An API Id back to nation/region name
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Formated string convert back to name</returns>
        internal static string FromID(string text)
        {
            return text?.Trim().ToLower().Replace('_', ' ');
        }
    }
}
