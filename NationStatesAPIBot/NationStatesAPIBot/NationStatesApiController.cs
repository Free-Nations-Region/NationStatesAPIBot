using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace NationStatesAPIBot
{
    internal class NationStatesApiController
    {
        private const string Source = "NationStatesApiController";
        internal DateTime lastAPIRequest;
        internal DateTime lastTelegramSending;
        internal DateTime lastAutomaticNewNationRequest;
        internal DateTime lastAutomaticRegionNationsRequest;
        /// <summary>
        /// Creates an HttpWebRequest targeted to NationStatesAPI
        /// </summary>
        /// <param name="parameters">The api parameters to pass into the request</param>
        /// <returns>A prepared HttpWebRequest ready to be executed</returns>
        private HttpWebRequest CreateApiRequest(string parameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"http://www.nationstates.net/cgi-bin/api.cgi?{parameters}");
            request.Method = "GET";
            request.UserAgent = ActionManager.NationStatesAPIUserAgent;
            return request;
        }
        /// <summary>
        /// Executes an provide HttpWebRequest targeted to NationStatesAPI
        /// </summary>
        /// <param name="webRequest">The HttpWebRequest to be executed </param>
        /// <param name="type">The Type of API Action to be executed</param>
        /// <param name="isScheduled">Flag that determines whether this request is automatically (e.g. by the recruiting process) executed or not</param>
        /// <returns>The response stream of the HttpWebRequest</returns>
        private async Task<Stream> ExecuteRequestAsync(HttpWebRequest webRequest, NationStatesApiRequestType type, bool isScheduled)
        {
            try
            {
                Log(LogSeverity.Debug, $"Waiting to execute {type}-Request. Once ActionManager grants the permit the request will be executed.");
                while (!ActionManager.IsNationStatesApiActionAllowed(type, isScheduled))
                {
                    await Task.Delay(1000); //Wait 1 second and try again
                }
                switch (type)
                {
                    case NationStatesApiRequestType.SendTelegram:
                        lastTelegramSending = DateTime.Now;
                        break;
                    case NationStatesApiRequestType.GetNewNations:
                        lastAutomaticNewNationRequest = DateTime.Now;
                        break;
                    case NationStatesApiRequestType.GetNationsFromRegion:
                        lastAutomaticRegionNationsRequest = DateTime.Now;
                        break;
                }

                lastAPIRequest = DateTime.Now;
                var response = await webRequest.GetResponseAsync();
                return response.GetResponseStream();
            }
            catch (Exception ex)
            {
                Log(LogSeverity.Error, ex.ToString());
                return Stream.Null;
            }
        }
        /// <summary>
        /// Executes an provide HttpWebRequest targeted to NationStatesAPI
        /// </summary>
        /// <param name="webRequest">The HttpWebRequest to be executed </param>
        /// <param name="type">The Type of API Action to be executed</param>
        /// <returns>The response stream of the HttpWebRequest</returns>
        private async Task<Stream> ExecuteRequestAsync(HttpWebRequest webRequest, NationStatesApiRequestType type)
        {
            return await ExecuteRequestAsync(webRequest, type, false);
        }
        /// <summary>
        /// Executes an provide HttpWebRequest targeted to NationStatesAPI
        /// </summary>
        /// <param name="webRequest">The HttpWebRequest to be executed </param>
        /// <param name="type">The Type of API Action to be executed</param>
        /// <param name="isScheduled">Flag that determines whether this request is automatically (e.g. by the recruiting process) executed or not</param>
        /// <returns>The text read from the response stream of the HttpWebRequest</returns>
        private async Task<string> ExecuteRequestWithTextResponseAsync(HttpWebRequest webRequest, NationStatesApiRequestType type, bool isScheduled)
        {
            using (var stream = await ExecuteRequestAsync(webRequest, type, isScheduled))
            {
                StreamReader streamReader = new StreamReader(stream);
                return await streamReader.ReadToEndAsync();
            }
        }
        /// <summary>
        /// Requests newly created Nations from NationStatesAPI
        /// </summary>
        /// <param name="isScheduled">Flag that determines whether this request is automatically (e.g. by the recruiting process) executed or not</param>
        /// <returns>List of nation names</returns>
        internal async Task<List<string>> RequestNewNationsAsync(bool isScheduled)
        {
            var request = CreateApiRequest($"q=newnations&v={ActionManager.API_VERSION}");
            XmlDocument newNationsXML = new XmlDocument();
            using (var stream = await ExecuteRequestAsync(request, NationStatesApiRequestType.GetNewNations, isScheduled))
            {
                if (stream.Length > 0)
                {
                    newNationsXML.Load(stream);
                    XmlNodeList newNationsXMLNodes = newNationsXML.GetElementsByTagName("NEWNATIONS");

                    List<string> newNations = newNationsXMLNodes[0].InnerText.Split(',').ToList().Select(nation => ToID(nation)).ToList();
                    return newNations;
                }
                else
                {
                    Log(LogSeverity.Debug, "Resolving 'RequestNewNations' with empty list because got empty stream returned. Check if an error occurred.");
                    return new List<string>();
                }
            }
        }
        /// <summary>
        /// Requests all nations from specific region
        /// </summary>
        /// <param name="region">the region from which the member nations should be requested</param>
        /// <param name="isScheduled">Flag that determines whether this request is automatically (e.g. by the recruiting process) executed or not</param>
        /// <returns>List of nation names</returns>
        internal async Task<List<string>> RequestNationsFromRegionAsync(string region, bool isScheduled)
        {
            var request = CreateApiRequest($"q=region={ToID(region)}&v={ActionManager.API_VERSION}");
            XmlDocument nationsXML = new XmlDocument();
            using (var stream = await ExecuteRequestAsync(request, NationStatesApiRequestType.GetNationsFromRegion, isScheduled))
            {
                if (stream.Length > 0)
                {
                    nationsXML.Load(stream);
                    XmlNodeList newNationsXMLNodes = nationsXML.GetElementsByTagName("NATIONS");

                    List<string> nations = newNationsXMLNodes[0].InnerText.Split(',').ToList().Select(nation => ToID(nation)).ToList();
                    return nations;
                }
                else
                {
                    Log(LogSeverity.Debug,"Finishing 'RequestNewNations' with empty list because got empty stream returned. Check if an error occurred.");
                    return new List<string>();
                }
            }
        }
        /// <summary>
        /// Send an (Recruitment-)Telegram to an specified recipient
        /// </summary>
        /// <param name="recipient">The name of the nation which should receive the telegram</param>
        /// <param name="telegramId">The name of the nation which should receive the telegram</param>
        /// <param name="isRecruitment">Flag that determines if the telegram is an recruitment telegram or not</param>
        /// <returns>If the telegram could be queued successfully</returns>
        internal async Task<bool> SendTelegramAsync(string recipient, string telegramId, bool isRecruitment)
        {
            try
            {
                var request = CreateApiRequest($"a=sendTG" +
                    $"&client={HttpUtility.UrlEncode(ActionManager.NationStatesClientKey)}" +
                    $"&tgid={HttpUtility.UrlEncode(ActionManager.NationStatesRecruitmentTelegramID)}" +
                    $"&key={HttpUtility.UrlEncode(ActionManager.NationStatesSecretKey)}" +
                    $"&to={HttpUtility.UrlEncode(ToID(recipient))}");
                var responseText = await ExecuteRequestWithTextResponseAsync(request, isRecruitment ?
                    NationStatesApiRequestType.SendRecruitmentTelegram :
                    NationStatesApiRequestType.SendTelegram, true); //isScheduled true -> Telegrams can not be send manually
                if (!responseText.Contains("queued"))
                    throw new Exception("NationStates reported an error: " + responseText);
                return true;
            }
            catch (Exception ex)
            {
                Log(LogSeverity.Error, ex.ToString());
                return false;
            }
        }
        /// <summary>
        /// Sends an Recruitmentelegram to an specified recipient
        /// </summary>
        /// <param name="recipient">The name of the nation which should receive the telegram</param>
        /// <returns>If the telegram could be queued successfully</returns>
        internal async Task<bool> SendTelegramAsync(string recipient)
        {
            return await SendTelegramAsync(recipient, ActionManager.NationStatesRecruitmentTelegramID, true);
        }
        /// <summary>
        /// Local Helper Method for logging
        /// </summary>
        /// <param name="severity">LogSeverity of the message to log</param>
        /// <param name="message">The message to log</param>
        private static void Log(LogSeverity severity, string message)
        {
            Task.Run(() => ActionManager.LoggerInstance.LogAsync(severity, Source, message));
        }
        /// <summary>
        /// Converts nation/region name to format that can be used on api calls
        /// </summary>
        /// <param name="text">The text to ensure format on</param>
        /// <returns>Formated string</returns>
        private static string ToID(string text)
        {
            return text.Trim().ToLower().Replace(' ', '_');
        }
        /// <summary>
        /// An API Id back to nation/region name
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Formated string convert back to name</returns>
        private static string FromID(string text)
        {
            return text.Trim().ToLower().Replace('_', ' ');
        }

    }
}
