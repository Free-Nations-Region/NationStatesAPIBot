using Discord;
using Discord.Commands;
using NationStatesAPIBot.Types;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NationStatesAPIBot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Diagnostics;

namespace NationStatesAPIBot.Commands.Stats
{
    public class BasicNationStats : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<BasicNationStats> _logger;
        private readonly NationStatesApiService _apiDataService;
        private readonly DumpDataService _dumpDataService;
        private readonly Random _rnd = new Random();
        private readonly CultureInfo _locale;
        private readonly RecruitmentService _recruitmentService;

        public BasicNationStats(ILogger<BasicNationStats> logger, NationStatesApiService apiService, DumpDataService dumpDataService, IOptions<AppSettings> config, RecruitmentService recruitmentService)
        {
            _logger = logger;
            _apiDataService = apiService;
            _dumpDataService = dumpDataService;
            _locale = config.Value.Locale;
            _recruitmentService = recruitmentService;
        }

        [Command("nation", false), Alias("n"), Summary("Returns Basic Stats about a specific nation")]
        public async Task GetBasicStatsAsync(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetNationStats);
            try
            {
                string nationName = string.Join(" ", args);
                _logger.LogInformation(id, LogMessageBuilder.Build(id, $"BasicNationStats for {nationName} requested."));
                XmlDocument nationStats = await _apiDataService.GetNationStatsAsync(nationName, id);
                if (nationStats != null)
                {
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
                    var residencyDbl = Convert.ToDouble(residency, _locale);
                    var residencyYears = (int) (residencyDbl / 365.242199);

                    var populationdbl = Convert.ToDouble(population);

                    var nationUrl = $"https://www.nationstates.net/nation={BaseApiService.ToID(nationName)}";
                    var regionUrl = $"https://www.nationstates.net/region={BaseApiService.ToID(region)}";

                    var builder = new EmbedBuilder();
                    builder.WithThumbnailUrl(flagUrl);
                    builder.WithTitle($"BasicStats for Nation");
                    builder.WithDescription($"**[{fullname}]({nationUrl})** {Environment.NewLine}" +
                        $"{(populationdbl / 1000.0 < 1 ? populationdbl : populationdbl / 1000.0).ToString(_locale)} {(populationdbl / 1000.0 < 1 ? "million" : "billion")} {demonymplural} | " +
                        $"Founded {founded} | " +
                        $"Last active {lastActivity}");
                    builder.AddField("Region",
                        $"[{region}]({regionUrl}) ", true);
                    int residencyDays = (int) (residencyDbl % 365.242199);
                    builder.AddField("Residency", $"Resident for " +
                        $"{(residencyYears < 1 ? "" : $"{residencyYears} year" + $"{(residencyYears > 1 ? "s" : "")}")} " +
                        $"{residencyDays} { (residencyDays != 1 ? $"days" : "day")}", true
                        );
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
                    builder.AddField(wa, $"{waVoteString} {endorsementCount} endorsements | {influenceValue} Influence ({Influence})", true);
                    builder.WithFooter(DiscordBotService.FooterString);
                    builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                    await ReplyAsync(embed: builder.Build());
                }
                else
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle($"Something went wrong.");
                    builder.WithDescription("Probably no such nation.");
                    await ReplyAsync(embed: builder.Build());
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        [Command("whoendorsed", false), Alias("we"), Summary("Returns all nations who endorsed the specified nation")]
        public async Task GetEndorsementsAsync(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetEndorsedBy);
            try
            {
                string nationName = string.Join(" ", args);
                XmlDocument nationStats = await _apiDataService.GetEndorsementsAsync(nationName, id);
                var endorsements = nationStats.GetElementsByTagName("ENDORSEMENTS")[0].InnerText;
                var builder = new EmbedBuilder();
                var nations = endorsements.Split(",").ToList();
                ;
                if (!string.IsNullOrWhiteSpace(endorsements))
                {
                    builder.WithTitle($"{nationName} was endorsed by {nations.Count} nations:");
                    StringBuilder sBuilder = new StringBuilder();
                    foreach (string name in nations)
                    {
                        sBuilder.Append(BaseApiService.FromID(name) + " ; ");
                    }
                    builder.WithDescription(sBuilder.ToString());
                }
                else
                {
                    builder.WithDescription("No one so far. Sorry :(");
                }
                builder.WithFooter(DiscordBotService.FooterString);
                builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                //ToDo: Maybe move to embed sender ?
                var e = builder.Build();
                if (e.Length >= 2000)
                {
                    _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Embeded has a length of {e.Length}"));
                }
                await ReplyAsync(embed: e);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        [Command("endorsedby", false), Alias("eb"), Summary("Returns all nations that where endorsed by the specified nation")]
        public async Task GetNationsendorsedbyAsync(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetEndorsedBy);
            string nationName = string.Join(" ", args);
            var builder = new EmbedBuilder();
            try
            {
                if (DumpDataService.IsUpdating)
                {
                    await ReplyAsync("Currently updating nation information. This may take a few minutes. You will be pinged once the information is available.");
                    builder.WithTitle($"{Context.User.Mention}/n");
                }
                var endorsed = await _dumpDataService.GetNationsEndorsedByAsync(nationName);
                if (endorsed == null)
                {
                    builder.WithDescription("No such nation.");
                    builder.WithFooter(DiscordBotService.FooterString);
                    builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                    await ReplyAsync(embed: builder.Build());
                    return;
                }
                builder.WithTitle($"{builder.Title}{nationName} has endorsed {endorsed.Count} nations:");
                if (endorsed.Count > 0)
                {
                    StringBuilder sBuilder = new StringBuilder();
                    foreach (var nation in endorsed)
                    {
                        sBuilder.Append(nation.NAME + " ; ");
                    }
                    builder.WithDescription(sBuilder.ToString());
                }
                else
                {
                    builder.WithDescription("No one so far.");
                }
                builder.WithFooter(DiscordBotService.FooterString);
                builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                //ToDo: Maybe move to embed sender ?
                var e = builder.Build();
                if (e.Length >= 2000)
                {
                    _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Embeded has a length of {e.Length}"));
                }
                await ReplyAsync(embed: e);
            }
            catch (InvalidOperationException ex)
            {
                builder.WithDescription(ex.Message);
                builder.WithFooter(DiscordBotService.FooterString);
                builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                var e = builder.Build();
                await ReplyAsync(embed: e);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        [Command("couldendorse", false), Alias("ce"), Summary("Returns all nations that could be endorsed by the specified nation")]
        public async Task GetNationsNotEndorsedbyAsync(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetNationsNotEndorsed);
            string nationName = string.Join(" ", args);
            bool mention = false;
            try
            {
                if (DumpDataService.IsUpdating)
                {
                    await ReplyAsync("Currently updating nation information. This may take a few minutes. You will be pinged once the information is available.");
                    mention = true;
                }
                var endorsed = await _dumpDataService.GetNationsNotEndorsedByAsync(nationName);
                if (endorsed == null)
                {
                    await ReplyAsync(embed: CouldEndorseEmbedBuilder("", "No such nation").Build());
                    return;
                }
                if (endorsed.Count > 0)
                {
                    StringBuilder sBuilder = new StringBuilder();
                    int length = 0;
                    int responsecounter = 1;
                    foreach (var nation in endorsed)
                    {
                        var toappend = nation.NAME + " ; ";
                        length += toappend.Length;
                        if (length < 1500)
                        {
                            sBuilder.Append(toappend);
                        }
                        else
                        {
                            var partbuilder = CouldEndorseEmbedBuilder($"{nationName} could endorse {endorsed.Count} more nations - Part {responsecounter}:", sBuilder.ToString());
                            await ReplyAsync(embed: partbuilder.Build());
                            responsecounter++;
                            length = 0;
                            sBuilder.Clear();
                        }
                    }
                    var builder = CouldEndorseEmbedBuilder($"{nationName} could endorse {endorsed.Count} more nations {(responsecounter > 1 ? $"- Part {responsecounter}:" : "")}", sBuilder.ToString());
                    await ReplyAsync(embed: builder.Build());
                }
                else
                {
                    var builder = CouldEndorseEmbedBuilder($"{nationName} could endorse {endorsed.Count} more nations:", "No one to endorse anymore. Good Job !");
                    await ReplyAsync(embed: builder.Build());
                }
                if (mention)
                {
                    await ReplyAsync($"{Context.User.Mention}");
                }
            }
            catch (InvalidOperationException ex)
            {
                var e = CouldEndorseEmbedBuilder("", ex.Message);
                await ReplyAsync(embed: e.Build());
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        private EmbedBuilder CouldEndorseEmbedBuilder(string title, string description)
        {
            var builder = new EmbedBuilder();
            builder.WithTitle(title);
            builder.WithDescription(description);
            builder.AddField("Datasource", "Dump", true);
            builder.AddField("As of", DateTime.UtcNow.Subtract(DumpDataService.LastDumpUpdateTimeUtc).ToString("h'h 'm'm 's's'") + " ago", true);
            builder.AddField("Next Update in", DumpDataService.NextDumpDataUpdate.ToString("h'h 'm'm 's's'"), true);
            builder.WithFooter(DiscordBotService.FooterString);
            builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
            return builder;
        }

        [Command("nationsdidnotendorse", false), Alias("nde"), Summary("Returns all nations that didn't endorse the specified nation")]
        public async Task GetNationsWhoDidNotEndorseAsync(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetNationsWhoDidNotEndorse);
            string nationName = string.Join(" ", args);
            var builder = new EmbedBuilder();
            try
            {
                if (DumpDataService.IsUpdating)
                {
                    await ReplyAsync("Currently updating nation information. This may take a few minutes. You will be pinged once the information is available.");
                    builder.WithTitle($"{Context.User.Mention}/n");
                }
                var endorsed = await _dumpDataService.GetNationsWhoDidNotEndorseNationAsync(nationName);
                if (endorsed == null)
                {
                    builder.WithDescription("No such nation.");
                    builder.WithFooter(DiscordBotService.FooterString);
                    builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                    await ReplyAsync(embed: builder.Build());
                    return;
                }
                builder.WithTitle($"{builder.Title}{nationName} could be endorsed by {endorsed.Count} more nations:");
                if (endorsed.Count > 0)
                {
                    StringBuilder sBuilder = new StringBuilder();
                    foreach (var nation in endorsed)
                    {
                        sBuilder.Append(nation.NAME + " ; ");
                    }
                    builder.WithDescription(sBuilder.ToString());
                }
                else
                {
                    builder.WithDescription("Everyone has endorsed that one. Hmm... :thinking:");
                }
                builder.WithFooter(DiscordBotService.FooterString);
                builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                //ToDo: Maybe move to embed sender ?
                var e = builder.Build();
                if (e.Length >= 2000)
                {
                    _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Embeded has a length of {e.Length}"));
                }
                await ReplyAsync(embed: e);
            }
            catch (InvalidOperationException ex)
            {
                builder.WithDescription(ex.Message);
                builder.WithFooter(DiscordBotService.FooterString);
                builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                var e = builder.Build();
                await ReplyAsync(embed: e);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        //[Command("wa", false, RunMode = RunMode.Async), Alias("wa"), Summary("Returns all nations that didn't endorse the specified nation")]
        public async Task GetWaAsync()
        {
            //var wa = await _dumpDataService.GetAllWa();
            //var nations = await _dumpDataService.GetWAOfRegion("the_free_nations_region");
            var region = await _dumpDataService.GetRegionAsync("the_free_nations_region");
            var strings = region.NATIONS.Select(n => n.NAME);

            var delegateNation = await _dumpDataService.GetNationAsync(region.DELEGATE);
            var elig = delegateNation.ENDORSEMENTS;
            elig.Add(region.DELEGATE);
            var res = string.Join(Environment.NewLine, elig);
            Console.WriteLine(res);
            //await ReplyAsync("Got all WA Nations");
            Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            //int start = 4500;
            //StringBuilder builder = new StringBuilder();
            //int writecounter = 0;
            //for (int i = start; i < strings.Count; i++)
            //{
            //    var currentString = strings.ElementAt(i);
            //    using (StreamWriter writer = new StreamWriter("wareceiver.txt", true))
            //    {
            //        var tickspernation = (i - start) > 0 ? TimeSpan.FromTicks(stopwatch.Elapsed.Ticks / (i - start)) : TimeSpan.Zero;
            //        var totalinticks = tickspernation.Multiply(strings.Count - start);
            //        var finishIn = totalinticks.Subtract(stopwatch.Elapsed);
            //        if (i > start && i % 500 == 0)
            //        {
            //            string info = $"Checked {i} / {strings.Count}. Expect finish in: {finishIn}, Avg. Time per check: {tickspernation}, Total estimate: {totalinticks}";
            //            try
            //            {
            //                await ReplyAsync(info);
            //            }
            //            catch (Exception e)
            //            {
            //                _logger.LogCritical(e, "Error :(");
            //                _logger.LogWarning(info);
            //            }
            //        }
            //        if (await _recruitmentService.WouldReceiveTelegram(currentString, false))
            //        {
            //            writecounter++;
            //            builder.Append($"{currentString}, ");
            //            if (writecounter % 8 == 0)
            //            {
            //                await writer.WriteLineAsync(builder.ToString());
            //                writecounter = 0;
            //                builder.Clear();
            //                _logger.LogInformation($"Line completed: {i} / {strings.Count - start} Expect finish in: {finishIn}, Avg. Time per check: {tickspernation}, Total estimate: {totalinticks}");
            //            }

            //        }
            //    }
            //}
        }
    }
}