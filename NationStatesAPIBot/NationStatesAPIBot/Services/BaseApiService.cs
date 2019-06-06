using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
            var logId = eventId != null ? (EventId)eventId : LogEventIdProvider.GetRandomLogEventId(); ;
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
                    _logger.LogDebug(logId, LogMessageBuilder.Build(logId, $"Request finished with response: {response.StatusCode}: {response.ReasonPhrase}"));
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
            return await response.Content.ReadAsStreamAsync();
        }

        protected async Task<XmlDocument> ExecuteRequestWithXmlResult(string url, EventId? eventId)
        {
            using (var stream = await ExecuteRequestWithStreamResult(url, eventId))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(stream);
                return xml;
            }
        }

        protected string BuildApiRequestUrl(string parameters)
        {
            return $"http://www.nationstates.net/cgi-bin/api.cgi?{parameters}";
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
                }
                else if (type == NationStatesDumpType.Regions)
                {
                    url += "regions.xml.gz";
                }
                else
                {
                    throw new NotImplementedException($"Retrieval for DumpType {type} not implemented yet.");
                }
                using (var stream = await ExecuteRequestWithStreamResult(url, eventId))
                {
                    return new GZipStream(stream, CompressionMode.Decompress);
                }
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
            return text?.Trim().ToLower().Replace(' ', '_');
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
