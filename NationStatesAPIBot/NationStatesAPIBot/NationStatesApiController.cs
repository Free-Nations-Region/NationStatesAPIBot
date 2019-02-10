using Discord;
using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
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
        internal DateTime lastAutomaticNewNationsRequest;
        internal DateTime lastAutomaticRegionNationsRequest;
        internal bool isRecruiting { get; set; }
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
                while (!ActionManager.IsNationStatesApiActionReady(type, isScheduled))
                {
                    await Task.Delay(1000); //Wait 1 second and try again
                }
                switch (type)
                {
                    case NationStatesApiRequestType.SendTelegram:
                        lastTelegramSending = DateTime.Now;
                        break;
                    case NationStatesApiRequestType.GetNewNations:
                        lastAutomaticNewNationsRequest = DateTime.Now;
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
                return null;
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
                if (stream != null)
                {
                    newNationsXML.Load(stream);
                    XmlNodeList newNationsXMLNodes = newNationsXML.GetElementsByTagName("NEWNATIONS");

                    List<string> newNations = newNationsXMLNodes[0].InnerText.Split(',').ToList().Select(nation => ToID(nation)).ToList();
                    return newNations;
                }
                else
                {
                    Log(LogSeverity.Warning, "Finishing 'RequestNewNations' with empty list because got empty stream returned. Check if an error occurred.");
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
            var id = ToID(region);
            var request = CreateApiRequest($"region={id}&q=nations&v={ActionManager.API_VERSION}");
            XmlDocument nationsXML = new XmlDocument();
            using (var stream = await ExecuteRequestAsync(request, NationStatesApiRequestType.GetNationsFromRegion, isScheduled))
            {
                if (stream != null)
                {
                    nationsXML.Load(stream);
                    XmlNodeList newNationsXMLNodes = nationsXML.GetElementsByTagName("NATIONS");

                    List<string> nations = newNationsXMLNodes[0].InnerText.Split(':').ToList().Select(nation => ToID(nation)).ToList();
                    return nations;
                }
                else
                {
                    Log(LogSeverity.Warning, "Finishing 'RequestNationsFromRegion' with empty list because got empty stream returned. Check if an error occurred.");
                    return new List<string>();
                }
            }
        }
        internal List<string> MatchNationsAgainstKnownNations(List<string> newNations, string StatusName)
        {
            Log(LogSeverity.Debug, "Match Nations - List, string");
            return MatchNationsAgainstKnownNations(newNations, StatusName, null);
        }
        internal List<string> MatchNationsAgainstKnownNations(List<string> newNations, string StatusName, string StatusDescription)
        {
            using (var context = new BotDbContext())
            {
                List<string> current;
                Log(LogSeverity.Debug, "Match Nations entered");
                if (string.IsNullOrWhiteSpace(ToID(StatusDescription)))
                {
                    current = context.Nations.Where(n => n.Status.Name == StatusName).Select(n => n.Name).ToList();
                }
                else
                {
                    current = context.Nations.Where(n => n.Status.Name == StatusName && n.Status.Description == ToID(StatusDescription)).Select(n => n.Name).ToList();
                }
                Log(LogSeverity.Debug, "Matched Nations - done");
                return newNations.Except(current).ToList();
            }
        }

        internal async Task AddToPending(List<string> newNations)
        {
            using (var context = new BotDbContext())
            {
                Log(LogSeverity.Debug, "AddToPending");
                var status = await context.NationStatuses.FirstOrDefaultAsync(n => n.Name == "pending");
                if (status == null)
                {
                    status = new NationStatus() { Name = "pending" };
                    await context.NationStatuses.AddAsync(status);
                }
                foreach (string name in newNations)
                {
                    await context.Nations.AddAsync(new Nation() { Name = name, StatusTime = DateTime.UtcNow, Status = status });
                }
                Log(LogSeverity.Debug, "Added to Pending");
                await context.SaveChangesAsync();
                Log(LogSeverity.Debug, "Changes saved");
            }
        }

        /// <summary>
        /// Compares new nations to stored nations, adds new ones and purges old ones that aren't members anymore
        /// </summary>
        /// <param name="newNations">List of new nation names</param>
        /// <param name="regionName">The region name to be used</param>
        /// <returns>An Tuple Item1 = joined Nations count, Item2 = left Nations count</returns>
        internal async Task<Tuple<int, int>> SyncRegionMembersWithDatabase(List<string> newNations, string regionName)
        {
            using (var context = new BotDbContext())
            {
                var joined = MatchNationsAgainstKnownNations(newNations, "member", regionName);
                var old = context.Nations.Where(n => n.Status.Name == "member" && n.Status.Description == ToID(regionName)).Select(n => n.Name).ToList();
                var currentWithOutJoined = newNations.Except(joined);
                var left = old.Except(currentWithOutJoined);
                var leftNations = context.Nations.Where(n => left.Contains(n.Name)).ToList();
                if (leftNations.Count > 0)
                {
                    context.RemoveRange(leftNations.ToArray());
                }
                if (joined.Count > 0)
                {
                    var status = await context.NationStatuses.FirstOrDefaultAsync(n => n.Name == "member" && n.Description == ToID(regionName));
                    if (status == null)
                    {
                        status = new NationStatus() { Name = "member", Description = ToID(regionName) };
                        await context.NationStatuses.AddAsync(status);
                        //await context.SaveChangesAsync();
                    }
                    foreach (string name in joined)
                    {
                        await context.Nations.AddAsync(new Nation() { Name = name, StatusTime = DateTime.UtcNow, Status = status });
                        //await context.SaveChangesAsync();
                    }
                }
                await context.SaveChangesAsync();
                return new Tuple<int, int>(joined.Count, leftNations.Count);
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
            return text?.Trim().ToLower().Replace(' ', '_');
        }
        /// <summary>
        /// An API Id back to nation/region name
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Formated string convert back to name</returns>
        private static string FromID(string text)
        {
            return text?.Trim().ToLower().Replace('_', ' ');
        }

        internal async Task StartRecruitingAsync()
        {
            Log(LogSeverity.Info, "Starting Recruitment process.");
            await Task.Delay(1000); //To-Do
        }

        internal async Task StopRecruitingAsync()
        {
            Log(LogSeverity.Info, "Stopping Recruitment process.");
            await Task.Delay(1000); //To-Do
        }
    }
}
