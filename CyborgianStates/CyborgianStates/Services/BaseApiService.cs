using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CyborgianStates.Types;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace CyborgianStates.Services
{
    public class BaseApiService
    {
        protected ILogger<BaseApiService> Logger { get; }
        protected AppSettings Config { get; }
        protected DateTime LastAPIRequest { get; set; }
        protected DateTime LastTelegramSending { get; set; }
        protected DateTime LastAutomaticNewNationsRequest { get; set; }
        protected DateTime LastAutomaticRegionNationsRequest { get; set; }
        public BaseApiService(IOptions<AppSettings> config, ILogger<BaseApiService> logger)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            Logger = logger;
            Config = config.Value;
        }
        protected async Task<HttpResponseMessage> ExecuteGetRequest(Uri url, EventId? eventId)
        {
            bool releaseId = false;
            var logId = eventId != null ? (EventId)eventId : LogEventIdProvider.GetRandomLogEventId();
            LastAPIRequest = DateTime.UtcNow;
            if (eventId == null)
            {
                releaseId = true;
            }
            try
            {
                using (var client = new HttpClient())
                {
                    Logger.LogDebug(logId, LogMessageBuilder.Build(logId, $"Executing GET-Request to {url}"));
                    client.DefaultRequestHeaders.Add("User-Agent", $"CyborgianStates/{AppSettings.VERSION}");
                    client.DefaultRequestHeaders.Add("User-Agent", $"(contact {Config.Contact};)");
                    var response = await client.GetAsync(url).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.LogError(logId, LogMessageBuilder.Build(logId, $"Request finished with response: {(int)response.StatusCode}: {response.ReasonPhrase}"));
                    }
                    else
                    {
                        Logger.LogDebug(logId, LogMessageBuilder.Build(logId, $"Request finished with response: {(int)response.StatusCode}: {response.ReasonPhrase}"));
                    }
                    if ((int)response.StatusCode == 429)
                    {
                        Logger.LogWarning(logId, LogMessageBuilder.Build(logId, $"Retry in {response.Headers.RetryAfter.Delta} seconds."));
                    }
                    return response;
                }
            }
            catch (ArgumentNullException ex)
            {
                Logger.LogCritical(logId, ex, LogMessageBuilder.Build(logId, $"A critical error occured.{Environment.NewLine}{ex}"));
                return null;
            }
            catch (HttpRequestException ex)
            {
                Logger.LogCritical(logId, ex, LogMessageBuilder.Build(logId, $"A critical error occured.{Environment.NewLine}{ex}"));
                return null;
            }
            finally
            {
                if (releaseId)
                {
                    LogEventIdProvider.ReleaseEventId(logId);
                }
            }
        }

        protected async Task<Stream> ExecuteRequestWithStreamResult(Uri url, EventId? eventId)
        {
            var response = await ExecuteGetRequest(url, eventId);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                throw new HttpRequestException($"A GET request to '{url}' failed with an unexpected error.");
            }
        }

        protected async Task<XmlDocument> ExecuteRequestWithXmlResult(Uri url, EventId eventId)
        {
            using (var stream = await ExecuteRequestWithStreamResult(url, eventId).ConfigureAwait(false))
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
                catch (XmlException ex)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        Logger.LogCritical(eventId, ex, LogMessageBuilder.Build(eventId, $"Some critical error with xml occured. {Environment.NewLine}XML: {await reader.ReadToEndAsync().ConfigureAwait(false)}"));
                    }
                    return null;
                }
            }
        }

        protected static Uri BuildApiRequestUrl(string parameters)
        {
            return new Uri($"http://www.nationstates.net/cgi-bin/api.cgi?{parameters}&v={AppSettings.API_VERSION}");
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
                    Logger.LogInformation(eventId, LogMessageBuilder.Build(eventId, "Retrieval of latest Nation dump requested"));
                }
                else if (type == NationStatesDumpType.Regions)
                {
                    url += "regions.xml.gz";
                    Logger.LogInformation(eventId, LogMessageBuilder.Build(eventId, "Retrieval of latest Region dump requested"));
                }
                else
                {
                    throw new NotImplementedException($"Retrieval for DumpType {type} not implemented yet.");
                }
                var stream = await ExecuteRequestWithStreamResult(new Uri(url), eventId).ConfigureAwait(false);
                var compressed = new GZipStream(stream, CompressionMode.Decompress);
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
