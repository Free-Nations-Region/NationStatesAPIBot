using Discord;
using Discord.Commands;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NationStatesAPIBot.Commands.Stats
{
    public class UpdateTime : ModuleBase<SocketCommandContext>
    {
        [Command("updatetime", false), Alias("ut"), Summary("Returns Updatetime Stats about a specific region")]
        public async Task GetBasicStats(params string[] args)
        {
            try
            {
                if (PermissionManager.IsAllowed(PermissionType.AccessUpdateTime, Context.User))
                {
                    string regionName = string.Join(" ", args);
                    await ActionManager.LoggerInstance.LogAsync(LogSeverity.Info, "BasicRegionStats", $"BasicRegionStats for {regionName} requested.");
                    var request = ActionManager.NationStatesApiController.CreateApiRequest($"region={NationStatesApiController.ToID(regionName)}&q=name+numnations+lastupdate+founder+delegate+delegateauth+flag+tags");
                    XmlDocument regionStats = new XmlDocument();
                    Random _rnd = new Random();
                    using (var stream = await ActionManager.NationStatesApiController.ExecuteRequestAsync(request, NationStatesApiRequestType.GetRegionStats))
                    {
                        if (stream != null)
                        {
                            regionStats.Load(stream);
                            var name = regionStats.GetElementsByTagName("NAME")[0].InnerText;
                            var numnations = regionStats.GetElementsByTagName("NUMNATIONS")[0].InnerText;
                            var wadelegate = regionStats.GetElementsByTagName("DELEGATE")[0].InnerText;
                            var delegateAuth = regionStats.GetElementsByTagName("DELEGATEAUTH")[0].InnerText;
                            var founder = regionStats.GetElementsByTagName("FOUNDER")[0].InnerText;
                            var flagUrl = regionStats.GetElementsByTagName("FLAG")[0].InnerText;
                            var tags = regionStats.GetElementsByTagName("TAGS")[0].ChildNodes;
                            var lastUpdate = regionStats.GetElementsByTagName("LASTUPDATE")[0].InnerText;
                            var lastUpdateTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(lastUpdate));
                            var tagList = "";
                            for (int i = 0; i < tags.Count; i++)
                            {
                                tagList += NationStatesApiController.FromID(tags.Item(i).InnerText) + ", ";
                            }
                            tagList = tagList.Remove(tagList.Length - 2);
                            var regionUrl = $"https://www.nationstates.net/region={NationStatesApiController.ToID(regionName)}";



                            var builder = new EmbedBuilder();
                            builder.WithThumbnailUrl(flagUrl);
                            builder.WithTitle($"UpdateTime Stats for Region");
                            builder.WithDescription($"**[{name}]({regionUrl})** {Environment.NewLine}" +
                                $"[{numnations} nations]({regionUrl}/page=list_nations)");
                            builder.AddField("Founder", $"[{await GetFullNationName(founder)}](https://www.nationstates.net/nation={NationStatesApiController.ToID(founder)})");
                            builder.AddField("Delegate", $"[{await GetFullNationName(wadelegate)}](https://www.nationstates.net/nation={NationStatesApiController.ToID(wadelegate)}) | {(delegateAuth.Contains('X') ? "EXECUTIVE" : "NON-EXECUTIVE")}");
                            builder.AddField("Tag-Values", $"Invader: {tagList.Contains("invader")} | Founderless: {tagList.Contains("founderless")} | Passworded: {tagList.Contains("passworded")} ");
                            builder.AddField("Tags", tagList);
                            builder.AddField("LastUpdate", GetLastUpdateString(lastUpdateTime));
                            builder.WithFooter($"NationStatesApiBot {Program.versionString} by drehtisch");
                            builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                            await ReplyAsync(embed: builder.Build());
                        }
                        else
                        {
                            await ActionManager.LoggerInstance.LogAsync(LogSeverity.Warning, "BasicNationStats", "Tried executing request. Return stream were null. Check if an error occurred");
                            var builder = new EmbedBuilder();
                            builder.WithTitle($"Something went wrong.");
                            builder.WithDescription("Probably no such region.");
                            await ReplyAsync(embed: builder.Build());
                        }
                    }
                }
                else
                {
                    await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
                }
            }
            catch (Exception ex)
            {
                await ActionManager.LoggerInstance.LogAsync(LogSeverity.Critical, "BasicNationStats", ex.ToString() + ex.StackTrace);
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        private static string GetLastUpdateString(DateTimeOffset lastUpdateTime)
        {
            if(lastUpdateTime.TimeOfDay < new TimeSpan(16, 0, 0))
            {
                return $"Major + {lastUpdateTime.Subtract(new TimeSpan(4, 0, 0)).TimeOfDay}";
            }
            else
            {
                return $"Minor + {lastUpdateTime.Subtract(new TimeSpan(16, 0, 0)).TimeOfDay}";
            }
        }

        private async Task<string> GetFullNationName(string name)
        {
            if (name != "0")
            {
                var request = ActionManager.NationStatesApiController.CreateApiRequest($"nation={NationStatesApiController.ToID(name)}&q=fullname");
                XmlDocument nationStats = new XmlDocument();
                using (var stream = await ActionManager.NationStatesApiController.ExecuteRequestAsync(request, NationStatesApiRequestType.GetNationStats))
                {
                    try
                    {
                        if (stream == null)
                        {
                            return "Error: CTE ?";
                        }
                        nationStats.Load(stream);
                        var fullName = nationStats.GetElementsByTagName("FULLNAME")[0].InnerText;
                        return fullName;
                    }
                    catch
                    {
                        return "Error: CTE ?";
                    }
                }
            }
            else
            {
                return "None";
            }
        }
    }
}
