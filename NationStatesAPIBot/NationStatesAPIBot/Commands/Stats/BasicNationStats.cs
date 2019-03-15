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
    public class BasicNationStats : ModuleBase<SocketCommandContext>
    {
        //TODO: Make CultureInfo configurable
        //TODO: Make Prefix configurable
        [Command("nation", false), Alias("n"), Summary("Returns Basic Stats about a specific nation")]
        public async Task GetBasicStats(params string[] args)
        {
            try
            {
                string nationName = string.Join(" ", args);
                await ActionManager.LoggerInstance.LogAsync(LogSeverity.Info, "BasicNationStats", $"BasicNationStats for {nationName} requested.");
                var request = ActionManager.NationStatesApiController.CreateApiRequest($"nation={NationStatesApiController.ToID(nationName)}&q=flag+wa+gavote+scvote+fullname+freedom+demonym2plural+category+population+region+founded+influence+lastactivity+census;mode=score;scale=0+1+2+65+66+80");
                XmlDocument nationStats = new XmlDocument();
                Random _rnd = new Random();
                using (var stream = await ActionManager.NationStatesApiController.ExecuteRequestAsync(request, NationStatesApiRequestType.GetNationStats))
                {
                    if (stream != null)
                    {
                        nationStats.Load(stream);
                        var demonymplural = nationStats.GetElementsByTagName("DEMONYM2PLURAL")[0].InnerText;
                        var category = nationStats.GetElementsByTagName("CATEGORY")[0].InnerText;
                        var flagUrl = nationStats.GetElementsByTagName("FLAG")[0].InnerText;
                        var fullname = nationStats.GetElementsByTagName("FULLNAME")[0].InnerText;
                        var population = nationStats.GetElementsByTagName("POPULATION")[0].InnerText;
                        var region = nationStats.GetElementsByTagName("REGION")[0].InnerText;
                        var founded = nationStats.GetElementsByTagName("FOUNDED")[0].InnerText;
                        var lastActivity = nationStats.GetElementsByTagName("LASTACTIVITY")[0].InnerText;
                        var Influence = nationStats.GetElementsByTagName("INFLUENCE")[0].InnerText;
                        var wa = nationStats.GetElementsByTagName("UNSTATUS")[0].InnerText;

                        var freedom = nationStats.GetElementsByTagName("FREEDOM")[0].ChildNodes;
                        var civilStr = freedom[0].InnerText;
                        var economyStr = freedom[1].InnerText;
                        var politicalStr = freedom[2].InnerText;

                        var census = nationStats.GetElementsByTagName("CENSUS")[0].ChildNodes;
                        var civilRights = census[0].ChildNodes[0].InnerText;
                        var economy = census[1].ChildNodes[0].InnerText;
                        var politicalFreedom = census[2].ChildNodes[0].InnerText;
                        var influenceValue = census[3].ChildNodes[0].InnerText;
                        var endorsementCount = census[4].ChildNodes[0].InnerText;
                        var residency = census[5].ChildNodes[0].InnerText;
                        var residecyDbl = Convert.ToDouble(residency, System.Globalization.CultureInfo.InvariantCulture);
                        var residencyYears = (int)(residecyDbl / 365.242199);

                        var populationdbl = Convert.ToDouble(population);

                        var nationUrl = $"https://www.nationstates.net/nation={NationStatesApiController.ToID(nationName)}";
                        var regionUrl = $"https://www.nationstates.net/region={NationStatesApiController.ToID(region)}";

                        var builder = new EmbedBuilder();
                        builder.WithThumbnailUrl(flagUrl);
                        builder.WithTitle($"BasicStats for Nation");
                        builder.WithDescription($"**[{fullname}]({nationUrl})** {Environment.NewLine}" +
                            $"{(populationdbl / 1000.0 < 1 ? populationdbl : populationdbl / 1000.0).ToString(new CultureInfo("en-US"))} {(populationdbl / 1000.0 < 1 ? "million" : "billion")} {demonymplural} | " +
                            $"Founded {founded} | " +
                            $"Last active {lastActivity}");
                        builder.AddField("Region",
                            $"[{region}]({regionUrl}) | " +
                            $"Resident since " +
                            $"{(residencyYears < 1 ? "" : $"{residencyYears} year" + $"{(residencyYears > 1 ? "s" : "")}")} " +
                            $"{(int)(residecyDbl % 365.242199) } days");
                        builder.AddField(category, $"C: {civilStr} ({civilRights}) | E: {economyStr} ({economy}) | P: {politicalStr} ({politicalFreedom})");
                        var waVoteString = "";
                        if (wa == "WA Member")
                        {
                            var gaVote = nationStats.GetElementsByTagName("GAVOTE")[0].InnerText;
                            var scVote = nationStats.GetElementsByTagName("SCVOTE")[0].InnerText;
                            if (!string.IsNullOrWhiteSpace(gaVote))
                            {
                                waVoteString += $"GA Vote: {gaVote} | ";
                            }
                            if (!string.IsNullOrWhiteSpace(scVote))
                            {
                                waVoteString += $"SC Vote: {scVote} | ";
                            }
                        }

                        builder.AddField(wa, waVoteString + $"{endorsementCount} endorsements | {influenceValue} Influence ({Influence})");
                        builder.WithFooter($"NationStatesApiBot {Program.versionString} by drehtisch");
                        builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                        await ReplyAsync(embed: builder.Build());
                    }
                    else
                    {
                        await ActionManager.LoggerInstance.LogAsync(LogSeverity.Warning, "BasicNationStats", "Tried executing request. Return stream were null. Check if an error occurred");
                        var builder = new EmbedBuilder();
                        builder.WithTitle($"Something went wrong.");
                        builder.WithDescription("Probably no such nation.");
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
    }
}
