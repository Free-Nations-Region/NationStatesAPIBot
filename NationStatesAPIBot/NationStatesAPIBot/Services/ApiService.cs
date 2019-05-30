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
    public class ApiService
    {
        private readonly ILogger<ApiService> _logger;
        private readonly AppSettings _config;
        public ApiService(IOptions<AppSettings> config, ILogger<ApiService> logger)
        {
            _logger = logger;
            _config = config.Value;
        }
        private async Task<HttpResponseMessage> ExecuteGetRequest(string url)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", _config.NATIONSTATES_API_USERAGENT);
                return await client.GetAsync(url);
            }
        }

        public async Task<Stream> ExecuteRequestWithStreamResult(string url)
        {
            using (var response = await ExecuteGetRequest(url))
            {
                return await response.Content.ReadAsStreamAsync();
            }
        }

        public async Task<XmlDocument> ExecuteRequestWithXmlResult(string url)
        {
            using (var stream = await ExecuteRequestWithStreamResult(url))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(stream);
                return xml;
            }
        }

        public string BuildApiRequestUrl(string parameters)
        {
            return $"http://www.nationstates.net/cgi-bin/api.cgi?{parameters}";
        }

        public async Task<GZipStream> GetNationStatesDumpStream(NationStatesDumpType type)
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
            using (var stream = await ExecuteRequestWithStreamResult(url))
            {
                return new GZipStream(stream, CompressionMode.Decompress);
            }
        }
    }
}
