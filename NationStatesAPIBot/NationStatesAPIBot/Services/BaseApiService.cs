using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NationStatesAPIBot.Interfaces
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
            var logId = eventId != null ? (EventId)eventId : LogEventIdGenerator.GetRandomLogEventId(); ;
            if (eventId == null)
            {
                releaseId = true;
            }
            try
            {
                using (var client = new HttpClient())
                {
                    _logger.LogDebug(logId, $"Executing Request to {url}");
                    client.DefaultRequestHeaders.Add("User-Agent", _config.NATIONSTATES_API_USERAGENT);
                    var response = await client.GetAsync(url);
                    _logger.LogDebug(logId, $"Request finished with response: {response.StatusCode}: {response.ReasonPhrase}");
                    return response;
                }
            }
            finally
            {
                if (releaseId)
                {
                    LogEventIdGenerator.ReleaseEventId(logId);
                }
            }
        }

        protected async Task<Stream> ExecuteRequestWithStreamResult(string url, EventId? eventId)
        {
            using (var response = await ExecuteGetRequest(url, eventId))
            {
                return await response.Content.ReadAsStreamAsync();
            }
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
            var eventId = LogEventIdGenerator.GetRandomLogEventId();
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
                LogEventIdGenerator.ReleaseEventId(eventId);
            }
        }
    }
}
