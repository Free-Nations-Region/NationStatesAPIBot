using CyborgianStates.Services;
using CyborgianStates.Types;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates.Commands
{
    public class EndorsedByCommand : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<WhoEndorsedCommand> _logger;
        private readonly DumpDataService _dumpDataService;
        private readonly Random _rnd = new Random();

        public EndorsedByCommand(ILogger<WhoEndorsedCommand> logger, DumpDataService dumpDataService)
        {
            _logger = logger;
            _dumpDataService = dumpDataService;
        }

        [Command("endorsedby", false), Summary("Returns all nations that where endorsed by a nation")]
        public async Task GetNationsendorsedby(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetEndorsedBy);
            try
            {
                string nationName = string.Join(" ", args);
                var builder = new EmbedBuilder();
                if (DumpDataService.IsUpdating)
                {
                    await ReplyAsync("Currently updating nation information. This may take a few minutes. You will be pinged once the information is available.");
                    builder.WithTitle($"{Context.User.Mention}/n");
                }
                var endorsed = await _dumpDataService.GetNationsEndorsedBy(nationName);
                builder.WithTitle($"{(string.IsNullOrWhiteSpace(builder.Title)?"":builder.Title)}{nationName} has endorsed {endorsed.Count} nations:");
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
