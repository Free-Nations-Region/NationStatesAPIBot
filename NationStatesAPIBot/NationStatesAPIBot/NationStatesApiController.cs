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
        internal DateTime lastAPIRequest;
        internal DateTime lastTelegramSending;
        internal DateTime lastAutomaticNewNationRequest;
        internal DateTime lastAutomaticRegionNationsRequest;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private HttpWebRequest CreateApiRequest(string parameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"http://www.nationstates.net/cgi-bin/api.cgi?{parameters}");
            request.Method = "GET";
            request.UserAgent = ActionManager.NationStatesAPIUserAgent;
            return request;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="webRequest"></param>
        /// <param name="type"></param>
        /// <param name="isScheduled"></param>
        /// <returns></returns>
        private async Task<Stream> ExecuteRequestAsync(HttpWebRequest webRequest, NationStatesApiRequestType type, bool isScheduled)
        {
            try
            {
                //To-Do: Make local log Method to reduce the writing effort.
                await ActionManager.LoggerInstance.LogAsync(Discord.LogSeverity.Debug, "NationStatesApiController", $"Waiting to execute {type}-Request. Once ActionManager grants the permit the request will be executed.");
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
                await ActionManager.LoggerInstance.LogAsync(Discord.LogSeverity.Error, "NationStatesApiController", ex.ToString());
                return Stream.Null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="webRequest"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private async Task<Stream> ExecuteRequestAsync(HttpWebRequest webRequest, NationStatesApiRequestType type)
        {
            return await ExecuteRequestAsync(webRequest, type, false);
        }

        private async Task<string> ExecuteRequestWithTextResponseAsync(HttpWebRequest webRequest, NationStatesApiRequestType type, bool isScheduled)
        {
            using (var stream = await ExecuteRequestAsync(webRequest, type, isScheduled))
            {
                StreamReader streamReader = new StreamReader(stream);
                return await streamReader.ReadToEndAsync();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal async Task<List<string>> RequestNewNations(bool isScheduled)
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
                    await ActionManager.LoggerInstance.LogAsync(Discord.LogSeverity.Debug, "NationStatesApiController", "Resolving 'RequestNewNations' with empty list because got empty stream returned. Check if an error occurred.");
                    return new List<string>();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal async Task<List<string>> RequestNationsFromRegion(string region, bool isScheduled)
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
                    await ActionManager.LoggerInstance.LogAsync(Discord.LogSeverity.Debug, "NationStatesApiController", "Resolving 'RequestNewNations' with empty list because got empty stream returned. Check if an error occurred.");
                    return new List<string>();
                }
            }
        }

        internal async Task<bool> SendRecruitmentTelegram(string recipient)
        {
            try
            {
                //a=sendTG&client=" + HttpUtility.UrlEncode(clientKey) + "&tgid=" + HttpUtility.UrlEncode(telegramID) + "&key=" + HttpUtility.UrlEncode(secretKey) + "&to=" + HttpUtility.UrlEncode(ToID(recipient))
                var request = CreateApiRequest($"a=sendTG" +
                    $"&client={HttpUtility.UrlEncode(ActionManager.NationStatesClientKey)}" +
                    $"&tgid={HttpUtility.UrlEncode(ActionManager.NationStatesRecruitmentTelegramID)}" +
                    $"&key={HttpUtility.UrlEncode(ActionManager.NationStatesSecretKey)}" +
                    $"&to={HttpUtility.UrlEncode(ToID(recipient))}");
                var responseText = await ExecuteRequestWithTextResponseAsync(request, NationStatesApiRequestType.SendTelegram, true); //isScheduled true -> Telegrams can not be send manually
                if (!responseText.Contains("queued"))
                    throw new Exception("NationStates reported an error: " + responseText);

                return true;
            }catch(Exception ex)
            {
                await ActionManager.LoggerInstance.LogAsync(Discord.LogSeverity.Error, "NationStatesApiController", ex.ToString())
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string ToID(string text)
        {
            return text.Trim().ToLower().Replace(' ', '_');
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string FromID(string text)
        {
            return text.Trim().ToLower().Replace('_', ' ');
        }

    }
}
