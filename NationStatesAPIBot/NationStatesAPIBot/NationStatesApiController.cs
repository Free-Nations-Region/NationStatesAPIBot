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
        internal bool IsRecruiting { get; private set; }
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
                Log(LogSeverity.Verbose, $"Waiting to execute {type}-Request. Once ActionManager grants the permit the request will be executed.");
                while (!ActionManager.IsNationStatesApiActionReady(type, isScheduled))
                {
                    await Task.Delay(1000); //Wait 1 second and try again
                }
                switch (type)
                {
                    case NationStatesApiRequestType.SendTelegram:
                    case NationStatesApiRequestType.SendRecruitmentTelegram:
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
                if (stream != null)
                {
                    StreamReader streamReader = new StreamReader(stream);
                    return await streamReader.ReadToEndAsync();
                }
                else
                {
                    Log(LogSeverity.Warning, "Tried executing request. Return stream were null. Check if an error occurred");
                    return string.Empty;
                }
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
            return MatchNationsAgainstKnownNations(newNations, StatusName, null);
        }
        internal List<string> MatchNationsAgainstKnownNations(List<string> newNations, string StatusName, string StatusDescription)
        {
            using (var context = new BotDbContext())
            {
                List<string> current;
                if (string.IsNullOrWhiteSpace(ToID(StatusDescription)))
                {
                    current = context.Nations.Where(n => n.Status.Name == StatusName).Select(n => n.Name).ToList();
                }
                else
                {
                    current = context.Nations.Where(n => n.Status.Name == StatusName && n.Status.Description == ToID(StatusDescription)).Select(n => n.Name).ToList();
                }

                return newNations.Except(current).ToList();
            }
        }

        internal async Task AddToPending(List<string> newNations)
        {
            int counter = 0;
            using (var context = new BotDbContext())
            {
                var status = await context.NationStatuses.FirstOrDefaultAsync(n => n.Name == "pending");
                if (status == null)
                {
                    status = new NationStatus() { Name = "pending" };
                    await context.NationStatuses.AddAsync(status);
                }
                List<Nation> notAddableNations = GetNationsByStatusName("send");
                notAddableNations.AddRange(GetNationsByStatusName("skipped"));
                foreach (string name in newNations)
                {
                    if (!notAddableNations.Exists(n => n.Name == name))
                    {
                        await context.Nations.AddAsync(new Nation() { Name = name, StatusTime = DateTime.UtcNow, Status = status });
                        counter++;
                    }
                }
                Log(LogSeverity.Verbose, $"{counter} nations added to pending");
                await context.SaveChangesAsync();
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
                    }
                    foreach (string name in joined)
                    {
                        await context.Nations.AddAsync(new Nation() { Name = name, StatusTime = DateTime.UtcNow, Status = status });
                    }
                }
                await context.SaveChangesAsync();
                return new Tuple<int, int>(joined.Count, leftNations.Count);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="telegramId"></param>
        /// <param name="isRecruitment"></param>
        /// <param name="isScheduled"></param>
        /// <returns></returns>
        internal async Task<bool> SendTelegramAsync(string recipient, string telegramId, string secretKey, bool isRecruitment, bool isScheduled)
        {
            try
            {
                Log(LogSeverity.Verbose, $"Sending Telegram to {recipient} scheduled: {isScheduled} recruitment: {isRecruitment}");
                var request = CreateApiRequest($"a=sendTG" +
                    $"&client={HttpUtility.UrlEncode(ActionManager.NationStatesClientKey)}" +
                    $"&tgid={HttpUtility.UrlEncode(telegramId)}" +
                    $"&key={HttpUtility.UrlEncode(secretKey)}" +
                    $"&to={HttpUtility.UrlEncode(ToID(recipient))}");
                var responseText = await ExecuteRequestWithTextResponseAsync(request, isRecruitment ?
                    NationStatesApiRequestType.SendRecruitmentTelegram :
                    NationStatesApiRequestType.SendTelegram, isScheduled);
                if (!string.IsNullOrWhiteSpace(responseText) && responseText.Contains("queued"))
                {
                    Log(LogSeverity.Verbose, "Telegram was queued successfully.");
                    return true;
                }
                else
                {
                    throw new Exception("NationStates reported an error: " + responseText);
                }

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
        private async Task<bool> SendRecruitmentTelegramAsync(string recipient)
        {
            return await SendTelegramAsync(recipient, ActionManager.NationStatesRecruitmentTelegramID, ActionManager.NationStatesRecruitmentTGSecretKey, true, true);
        }

        private static void Log(LogSeverity severity, string source, string message)
        {
            Task.Run(async () => await ActionManager.LoggerInstance.LogAsync(severity, $"{Source} - {source}", message));
        }

        private static void Log(LogSeverity severity, string message)
        {
            Task.Run(async () => await ActionManager.LoggerInstance.LogAsync(severity, Source, message));
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
            IsRecruiting = true;
            RecruitAsync();
            await Task.Delay(1000); //To-Do
        }

        internal async Task StopRecruitingAsync()
        {
            Log(LogSeverity.Info, "Stopping Recruitment process.");
            IsRecruiting = false;
            await Task.Delay(1000); //To-Do
        }

        private async Task SetNationStatusToSendAsync(Nation nation)
        {
            using (var dbContext = new BotDbContext())
            {
                var status = await dbContext.NationStatuses.FirstOrDefaultAsync(n => n.Name == "send");
                if (status == null)
                {
                    status = new NationStatus() { Name = "send" };
                    await dbContext.NationStatuses.AddAsync(status);
                    await dbContext.SaveChangesAsync();
                }
                nation.Status = status;
                nation.StatusId = status.Id;
                nation.StatusTime = DateTime.UtcNow;
                dbContext.Nations.Update(nation);
                await dbContext.SaveChangesAsync();
            }
        }

        private async Task SetNationStatusToSkippedAsync(Nation nation)
        {
            using (var dbContext = new BotDbContext())
            {
                var status = await dbContext.NationStatuses.FirstOrDefaultAsync(n => n.Name == "skipped");
                if (status != null)
                {
                    status = new NationStatus() { Name = "skipped" };
                    await dbContext.NationStatuses.AddAsync(status);
                    await dbContext.SaveChangesAsync();
                }
                nation.Status = status;
                nation.StatusId = status.Id;
                nation.StatusTime = DateTime.UtcNow;
                dbContext.Nations.Update(nation);
                await dbContext.SaveChangesAsync();
            }
        }

        private async Task SetNationStatusToFailedAsync(Nation nation)
        {
            using (var dbContext = new BotDbContext())
            {
                var status = await dbContext.NationStatuses.FirstOrDefaultAsync(n => n.Name == "failed");
                if (status == null)
                {
                    status = new NationStatus() { Name = "failed" };
                    await dbContext.NationStatuses.AddAsync(status);
                    await dbContext.SaveChangesAsync();
                }
                nation.Status = status;
                nation.StatusId = status.Id;
                nation.StatusTime = DateTime.UtcNow;
                dbContext.Nations.Update(nation);
                await dbContext.SaveChangesAsync();
            }
        }

        private async Task RecruitAsync()
        {
            List<Nation> pendingNations = new List<Nation>();
            while (IsRecruiting)
            {
                if (pendingNations.Count == 0)
                {
                    pendingNations = GetNationsByStatusName("pending");
                }

                var picked = pendingNations.Take(1);
                var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                if (nation != null)
                {
                    //ToDo: Check if recipient would receive telegra
                    if (ActionManager.IsNationStatesApiActionReady(NationStatesApiRequestType.SendRecruitmentTelegram, true))
                    {
                        if (await SendRecruitmentTelegramAsync(nation.Name))
                        {
                            await SetNationStatusToSendAsync(nation);
                        }
                        else
                        {
                            await SetNationStatusToFailedAsync(nation);
                            Log(LogSeverity.Warning, "Recruitment", $"Telegram to {nation.Name} could not be send.");
                        }
                        pendingNations.Remove(nation);
                    }

                }
                else
                {
                    Log(LogSeverity.Warning, "Pending Nations empty can not send telegram: No recipient."); //To-Do: Send alert to recruiters
                }
                if (ActionManager.IsNationStatesApiActionReady(NationStatesApiRequestType.GetNewNations, true))
                {
                    var result = await ActionManager.NationStatesApiController.RequestNewNationsAsync(true);
                    var newnations = ActionManager.NationStatesApiController.MatchNationsAgainstKnownNations(result, "pending");
                    await ActionManager.NationStatesApiController.AddToPending(newnations);
                }
                if (ActionManager.IsNationStatesApiActionReady(NationStatesApiRequestType.GetNationsFromRegion, true))
                {
                    string regionName = "the rejected realms";
                    var result = await ActionManager.NationStatesApiController.RequestNationsFromRegionAsync(regionName, true);
                    var joined = ActionManager.NationStatesApiController.MatchNationsAgainstKnownNations(result, "member", regionName);
                    var syncResult = await ActionManager.NationStatesApiController.SyncRegionMembersWithDatabase(result, regionName);
                    await ActionManager.NationStatesApiController.AddToPending(joined);
                }
                await Task.Delay(1000);
            }
        }

        private List<Nation> GetNationsByStatusName(string name)
        {
            using (var dbContext = new BotDbContext())
            {
                return dbContext.Nations.Where(n => n.Status.Name == name).ToList();
            }
        }
    }
}
