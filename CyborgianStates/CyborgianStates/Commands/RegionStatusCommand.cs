using CyborgianStates.Services;
using CyborgianStates.Types;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CyborgianStates.Commands
{
    public class RegionStatusCommand: ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<RegionStatusCommand> _logger;
        private readonly NationStatesApiService dataService;
        private readonly Random _rnd = new Random();
        public RegionStatusCommand(ILogger<RegionStatusCommand> logger, NationStatesApiService apiService)
        {
            _logger = logger;
            dataService = apiService;
        }

        [Command("region", false), Alias("r"), Summary("Returns Basic Stats about a specific nation")]
        public async Task GetBasicStats(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetRegionStats);
            try
            {
                string regionName = string.Join(" ", args);
                _logger.LogInformation(id, LogMessageBuilder.Build(id, $"BasicRegionStats for {regionName} requested."));
                XmlDocument regionStats = await dataService.GetRegionStatsAsync(regionName, id);

                if (regionStats != null)
                {
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
                        tagList += BaseApiService.FromID(tags.Item(i).InnerText) + ", ";
                    }
                    tagList = tagList.Remove(tagList.Length - 2);
                    var regionUrl = $"https://www.nationstates.net/region={BaseApiService.ToID(regionName)}";



                    var builder = new EmbedBuilder();
                    builder.WithThumbnailUrl(flagUrl);
                    builder.WithTitle($"BasicStats for Region");
                    builder.WithDescription($"**[{name}]({regionUrl})** {Environment.NewLine}" +
                        $"[{numnations} nations]({regionUrl}/page=list_nations) | {founded} | Power: {power}");
                    builder.AddField("Founder", $"[{await GetFullNationName(founder, id)}](https://www.nationstates.net/nation={BaseApiService.ToID(founder)})");
                    builder.AddField("Delegate", await GetDelegateNationString(wadelegate, id));
                    builder.WithFooter($"NationStatesApiBot {AppSettings.VERSION} by drehtisch");
                    builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                    await ReplyAsync(embed: builder.Build());
                }
                else
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle($"Something went wrong.");
                    builder.WithDescription("Probably no such region.");
                    await ReplyAsync(embed: builder.Build());
                }

            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        private async Task<string> GetFullNationName(string name, EventId eventId)
        {
            XmlDocument nationStats = await dataService.GetFullNationNameAsync(name, eventId);
            var fullName = nationStats.GetElementsByTagName("FULLNAME")[0].InnerText;
            return fullName;
        }

        private async Task<string> GetDelegateNationString(string name, EventId eventId)
        {
            XmlDocument nationStats = await dataService.GetDelegateString(name, eventId);
            var fullName = nationStats.GetElementsByTagName("FULLNAME")[0].InnerText;
            var influence = nationStats.GetElementsByTagName("INFLUENCE")[0].InnerText;
            var census = nationStats.GetElementsByTagName("CENSUS")[0].ChildNodes;
            var influenceValue = census[0].ChildNodes[0].InnerText;
            var endorsements = census[1].ChildNodes[0].InnerText;
            return $"[{fullName}](https://www.nationstates.net/nation={BaseApiService.ToID(name)}) | {endorsements} endorsements | {influenceValue} influence ({influence})";
        }
    }
}
