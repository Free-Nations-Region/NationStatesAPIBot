using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Services;
using NationStatesAPIBot.Types;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace NationStatesAPIBot.Commands.Stats
{
    public class BasicRegionStats : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<BasicRegionStats> _logger;
        private readonly NationStatesApiService _dataService;
        private readonly DumpDataService _dumpDataService;
        private readonly Random _rnd = new Random();
        private readonly string defaultRegionName;
        private readonly CultureInfo locale;
        public BasicRegionStats(ILogger<BasicRegionStats> logger, NationStatesApiService apiService, DumpDataService dumpDataService, IOptions<AppSettings> config)
        {
            _logger = logger;
            _dataService = apiService;
            _dumpDataService = dumpDataService;
            defaultRegionName = config.Value.NationStatesRegionName;
            locale = config.Value.Locale;
        }

        [Command("region", false), Alias("r"), Summary("Returns Basic Stats about a specific nation")]
        public async Task GetBasicStats(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetRegionStats);
            try
            {
                string regionName = string.Join(" ", args);
                if (string.IsNullOrWhiteSpace(regionName))
                {
                    regionName = defaultRegionName;
                }
                var mention = false;
                if (DumpDataService.IsUpdating)
                {
                    await ReplyAsync("Currently updating region information. This may take a few minutes. You will be pinged once the information is available.");
                    mention = true;
                }
                _logger.LogInformation(id, LogMessageBuilder.Build(id, $"BasicRegionStats for {regionName} requested."));
                XmlDocument regionStats = await _dataService.GetRegionStatsAsync(regionName, id);
                var region = await _dumpDataService.GetRegionAsync(BaseApiService.ToID(regionName));
                if (regionStats != null && region != null)
                {
                    var name = regionStats.GetElementsByTagName("NAME").Item(0)?.InnerText;
                    var numnations = regionStats.GetElementsByTagName("NUMNATIONS").Item(0)?.InnerText;
                    var wadelegate = regionStats.GetElementsByTagName("DELEGATE").Item(0)?.InnerText;
                    var founder = region.FOUNDER;
                    var founded = regionStats.GetElementsByTagName("FOUNDED").Item(0)?.InnerText;
                    var flagUrl = region.FLAG;
                    var power = regionStats.GetElementsByTagName("POWER").Item(0)?.InnerText;
                    var waNationCount = region.WANATIONS.Count();
                    var endoCount = region.WANATIONS.Sum(n => n.ENDORSEMENTS.Count);
                    var census = regionStats.GetElementsByTagName("CENSUS").Item(0)?.ChildNodes;
                    var regionalAvgInfluence = census.Item(0)?.ChildNodes.Item(0)?.InnerText;

                    var regionUrl = $"https://www.nationstates.net/region={BaseApiService.ToID(regionName)}";
                    var builder = new EmbedBuilder
                    {
                        ThumbnailUrl = flagUrl,
                        Title = name,
                        Url = regionUrl
                    };
                    if (!string.IsNullOrWhiteSpace(founder) && founder != "0")
                    {
                        string founderString = await GetFounderString(id, founder);
                        builder.AddField("Founder", $"{founderString}", true);
                    }
                    if (!string.IsNullOrWhiteSpace(founded) && founded != "0")
                    {
                        builder.AddField("Founded", $"{founded}", true);
                    }
                    builder.AddField("Nations", $"[{numnations}]({regionUrl}/page=list_nations)", true);
                    if (!string.IsNullOrWhiteSpace(regionalAvgInfluence) && double.TryParse(regionalAvgInfluence, NumberStyles.Number, locale, out double avgInfluenceValue) && int.TryParse(numnations, out int numnationsValue))
                    {
                        var powerValue = avgInfluenceValue * numnationsValue;
                        var powerValueString = powerValue > 1000 ? (powerValue / 1000.0).ToString("0.000", locale) + "k" : powerValue.ToString(locale);
                        builder.AddField("Regional Power", $"{power} | {powerValueString} Points", true);
                    }
                    else
                    {
                        builder.AddField("Regional Power", $"{power}", true);
                    }
                    var endoCountString = endoCount > 1000 ? (endoCount / 1000.0).ToString("0.000", locale) + "k" : endoCount.ToString(locale);
                    builder.AddField("World Assembly*", $"{waNationCount} member{(waNationCount > 1 ? "s" : string.Empty)} | {endoCountString} endos", true);
                    if (!string.IsNullOrWhiteSpace(wadelegate) && wadelegate != "0")
                    {
                        var delegatetuple = await GetDelegateNationString(wadelegate, id);
                        builder.AddField($"WA Delegate", $"[{delegatetuple.Item1}](https://www.nationstates.net/nation={BaseApiService.ToID(wadelegate)}) | {delegatetuple.Item2}");
                    }
                    builder.WithFooter(DiscordBotService.FooterString);
                    builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                    builder.AddField("Datasource", "API, * Dump", true);
                    builder.AddField("As of", "Just now, * " + DateTime.UtcNow.Subtract(DumpDataService.LastDumpUpdateTimeUtc).ToString("h'h 'm'm 's's'") + " ago", true);
                    builder.AddField("Next Update in","On demand, * " + DumpDataService.NextDumpDataUpdate.ToString("h'h 'm'm 's's'"), true);
                    await ReplyAsync(embed: builder.Build());
                }
                else
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle($"Something went wrong.");
                    builder.WithDescription("No API or No Dump data received. Probably no such region.");
                    await ReplyAsync(embed: builder.Build());
                }
                if (mention)
                {
                    await ReplyAsync($"{Context.User.Mention}");
                }

            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        private async Task<string> GetFounderString(EventId id, string founder)
        {
            var founderName = await GetFullNationName(founder, id);
            var founderUrl = $"https://www.nationstates.net/nation={BaseApiService.ToID(founder)}";
            var founderString = $"[{founderName}]({founderUrl})";
            return founderString;
        }

        private async Task<string> GetFullNationName(string name, EventId eventId)
        {
            XmlDocument nationStats = await _dataService.GetNationNameAsync(name, eventId);
            if (nationStats != null)
            {
                var fullNameRaw = nationStats.GetElementsByTagName("NAME");
                if (fullNameRaw.Count < 1)
                {
                    return "Unknown";
                }
                else
                {
                    var fullName = fullNameRaw.Item(0)?.InnerText;
                    return fullName;
                }
            }
            else
            {
                return "Unknown";
            }
        }

        private async Task<Tuple<string, string>> GetDelegateNationString(string name, EventId eventId)
        {

            XmlDocument nationStats = await _dataService.GetDelegateString(name, eventId);
            var fullName = nationStats.GetElementsByTagName("NAME").Item(0)?.InnerText;
            var influence = nationStats.GetElementsByTagName("INFLUENCE").Item(0)?.InnerText;
            var census = nationStats.GetElementsByTagName("CENSUS").Item(0)?.ChildNodes;
            var influenceValue = census.Item(0)?.ChildNodes.Item(0).InnerText;
            var endorsements = census.Item(1)?.ChildNodes.Item(0).InnerText;
            return new Tuple<string, string>(fullName, $" {endorsements} endorsements | {influenceValue} influence");
        }
    }
}
