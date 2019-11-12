using CyborgianStates.Services;
using CyborgianStates.Types;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CyborgianStates.Commands
{
    public class WhoEndorsedCommand : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<WhoEndorsedCommand> _logger;
        private readonly NationStatesApiService _apiDataService;
        private readonly Random _rnd = new Random();
        public WhoEndorsedCommand(ILogger<WhoEndorsedCommand> logger, NationStatesApiService apiService)
        {
            _logger = logger;
            _apiDataService = apiService;
        }

        [Command("whoendorsed", false), Summary("Returns all nations who endorsed a nation")]
        public async Task GetEndorsements(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetEndorsedBy);
            try
            {
                string nationName = string.Join(" ", args);
                XmlDocument nationStats = await _apiDataService.GetEndorsements(nationName, id);
                var endorsements = nationStats.GetElementsByTagName("ENDORSEMENTS")[0].InnerText;
                var builder = new EmbedBuilder();
                var nations = endorsements.Split(",").ToList(); ;
                builder.WithTitle($"{nationName} was endorsed by {nations.Count} nations:");
                if (!string.IsNullOrWhiteSpace(endorsements))
                {
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
                builder.WithFooter($"NationStatesApiBot {AppSettings.VERSION} by drehtisch");
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
    }
}
