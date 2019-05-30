using Discord;
using Discord.Commands;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace NationStatesAPIBot.Commands.Stats
{
    public class BasicRegionStats : ModuleBase<SocketCommandContext>
    {
        //[Command("region", false), Alias("r"), Summary("Returns Basic Stats about a specific nation")]
        public async Task GetBasicStats(params string[] args)
        {
            try
            {
                string regionName = string.Join(" ", args);
                await ActionManager.LoggerInstance.LogAsync(LogSeverity.Info, "BasicRegionStats", $"BasicRegionStats for {regionName} requested.");
                var request = ActionManager.NationStatesApiController.CreateApiRequest($"region={NationStatesApiController.ToID(regionName)}&q=name+numnations+founded+power+founder+delegate+flag+tags");
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
                        var founder = regionStats.GetElementsByTagName("FOUNDER")[0].InnerText;
                        var founded = regionStats.GetElementsByTagName("FOUNDED")[0].InnerText;
                        var flagUrl = regionStats.GetElementsByTagName("FLAG")[0].InnerText;
                        var power = regionStats.GetElementsByTagName("POWER")[0].InnerText;
                        var tags = regionStats.GetElementsByTagName("TAGS")[0].ChildNodes;
                        var tagList = "";
                        for (int i = 0; i < tags.Count; i++)
                        {
                            tagList += NationStatesApiController.FromID(tags.Item(i).InnerText) + ", ";
                        }
                        tagList = tagList.Remove(tagList.Length - 2);
                        var regionUrl = $"https://www.nationstates.net/region={NationStatesApiController.ToID(regionName)}";



                        var builder = new EmbedBuilder();
                        builder.WithThumbnailUrl(flagUrl);
                        builder.WithTitle($"BasicStats for Region");
                        builder.WithDescription($"**[{name}]({regionUrl})** {Environment.NewLine}"+
                            $"[{numnations} nations]({regionUrl}/page=list_nations) | {founded} | Power: {power}");
                        builder.AddField("Founder", $"[{await GetFullNationName(founder)}](https://www.nationstates.net/nation={NationStatesApiController.ToID(founder)})");
                        builder.AddField("Delegate", await GetDelegateNationString(wadelegate));
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
            catch (Exception ex)
            {
                await ActionManager.LoggerInstance.LogAsync(LogSeverity.Critical, "BasicNationStats", ex.ToString() + ex.StackTrace);
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        private async Task<string> GetFullNationName(string name)
        {
            var request = ActionManager.NationStatesApiController.CreateApiRequest($"nation={NationStatesApiController.ToID(name)}&q=fullname");
            XmlDocument nationStats = new XmlDocument();
            using (var stream = await ActionManager.NationStatesApiController.ExecuteRequestAsync(request, NationStatesApiRequestType.GetNationStats))
            {
                nationStats.Load(stream);
                var fullName = nationStats.GetElementsByTagName("FULLNAME")[0].InnerText;
                return fullName;
            }
        }

        private async Task<string> GetDelegateNationString(string name)
        {
            var request = ActionManager.NationStatesApiController.CreateApiRequest($"nation={NationStatesApiController.ToID(name)}&q=fullname+influence+census;mode=score;scale=65+66");
            XmlDocument nationStats = new XmlDocument();
            using (var stream = await ActionManager.NationStatesApiController.ExecuteRequestAsync(request, NationStatesApiRequestType.GetNationStats))
            {
                
                nationStats.Load(stream);
                var fullName = nationStats.GetElementsByTagName("FULLNAME")[0].InnerText;
                var influence = nationStats.GetElementsByTagName("INFLUENCE")[0].InnerText;
                var census = nationStats.GetElementsByTagName("CENSUS")[0].ChildNodes;
                var influenceValue = census[0].ChildNodes[0].InnerText;
                var endorsements = census[1].ChildNodes[0].InnerText;
                return $"[{fullName}](https://www.nationstates.net/nation={NationStatesApiController.ToID(name)}) | {endorsements} endorsements | {influenceValue} influence ({influence})";
            }
        }
    }
}
