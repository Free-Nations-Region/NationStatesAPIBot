using Discord.Commands;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using NationStatesAPIBot.Interfaces;
using Microsoft.Extensions.Logging;
using NationStatesAPIBot.Services;
using NationStatesAPIBot.Manager;
using System.Globalization;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace NationStatesAPIBot.Commands.Management
{
    public class Recruitment : ModuleBase<SocketCommandContext>
    {
        private readonly IPermissionManager _permManager;
        private readonly ILogger<Recruitment> _logger;
        private readonly RecruitmentService _recruitmentService;
        private readonly CultureInfo _locale;

        private static readonly string _actionQueued = $"Your action was queued successfully. Please be patient this may take a moment.";

        public Recruitment(IPermissionManager permissionManager, ILogger<Recruitment> logger, RecruitmentService recruitmentService, IOptions<AppSettings> config)
        {
            _permManager = permissionManager ?? throw new ArgumentNullException(nameof(permissionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _recruitmentService = recruitmentService;
            _locale = config.Value.Locale;
        }

        [Command("startRecruitment"), Summary("Starts the recruitment process")]
        public async Task DoStartRecruitmentAsync()
        {
            if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
            {
                if (RecruitmentService.RecruitmentStatus != "Disabled")
                {
                    _recruitmentService.StartRecruitment();
                    await ReplyAsync("Recruitment Process started.");
                }
                else
                {
                    await ReplyAsync("Can't start Recruitment because it is Disabled");
                }
            }
            else
            {
                await ReplyAsync(AppSettings._permissionDeniedResponse);
            }
        }

        [Command("stopRecruitment"), Summary("Stops the recruitment process")]
        public async Task DoStopRecruitmentAsync()
        {
            if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
            {
                if (RecruitmentService.IsRecruiting)
                {
                    _recruitmentService.StopRecruitment();
                    await ReplyAsync("Recruitment Process stopped.");
                }
                else
                {
                    await ReplyAsync("Recruitment Process isn't running.");
                }
            }
            else
            {
                await ReplyAsync(AppSettings._permissionDeniedResponse);
            }
        }

        [Command("rn"), Summary("Returns a list of nations which would receive an recruitment telegram")]
        public async Task DoGetRecruitableNationsAsync([Remainder, Summary("Number of nations to be returned")] int number)
        {
            //await ReplyAsync("The /rn command needed to be disabled because of major issues with the manual recruitment system. Drehtisch will fix those issue asap. Sorry :(");
            Console.ResetColor();
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.RNCommand);
            List<Nation> returnNations = new List<Nation>();
            try
            {
                if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
                {
                    if (!_recruitmentService.IsReceivingRecruitableNations)
                    {
                        try
                        {
                            if (number <= 120 && number > 0)
                            {
                                var currentRN = new RNStatus
                                {
                                    IssuedBy = Context.User.Username,
                                    FinalCount = number,
                                    StartedAt = DateTimeOffset.UtcNow,
                                    AvgTimePerFoundNation = TimeSpan.FromSeconds(2)
                                };
                                var channel = await Context.User.GetOrCreateDMChannelAsync();

                                _recruitmentService.StartReceiveRecruitableNations(currentRN);
                                await ReplyAsync($"{_actionQueued}{Environment.NewLine}{Environment.NewLine}You can request the status of this command using /rns. Finish expected in approx. (mm:ss): {currentRN.ExpectedIn():mm\\:ss}");
                                StringBuilder builder = new StringBuilder();
                                StringBuilder linkBuilder = new StringBuilder("https://www.nationstates.net/page=compose_telegram?tgto=");
                                var counter = 0;
                                await foreach (var nation in _recruitmentService.GetRecruitableNationsAsync(number, false, id))
                                {
                                    counter++;
                                    if (counter % 8 == 0)
                                    {
                                        builder.Append($"{nation.Name}");
                                        linkBuilder.Append($"{nation.Name}");
                                        await channel.SendMessageAsync(string.Concat("=====", Environment.NewLine, builder.ToString(), Environment.NewLine, Environment.NewLine, "Feeling lazy today? Use the link below.", Environment.NewLine, linkBuilder.ToString()));
                                        builder.Clear();
                                        linkBuilder.Clear();
                                        linkBuilder.Append("https://www.nationstates.net/page=compose_telegram?tgto=");
                                        _logger.LogInformation(id, LogMessageBuilder.Build(id, $"Dispatched {counter}/{number} nations to {Context.User.Username}"));
                                    }
                                    else
                                    {
                                        builder.Append($"{nation.Name}, ");
                                        linkBuilder.Append($"{nation.Name},");
                                    }
                                    await NationManager.SetNationStatusToAsync(nation, "reserved_manual");
                                }
                                if (builder.Length > 0)
                                {
                                    await channel.SendMessageAsync(string.Concat("=====", Environment.NewLine, builder.ToString(), Environment.NewLine, Environment.NewLine, "Feeling lazy today? Use the link below.", Environment.NewLine, linkBuilder.ToString()));
                                    _logger.LogInformation(id, LogMessageBuilder.Build(id, $"Dispatched {counter}/{number} nations to {Context.User.Username}"));
                                }
                                if (counter < number)
                                {
                                    await ReplyAsync($"Something went wrong didn't received as much nations as requested.");
                                }
                            }
                            else
                            {
                                await ReplyAsync($"{number} exceeds the maximum of 120 Nations (15 Telegrams a 8 recipients) to be returned.");
                            }
                        }
                        finally
                        {
                            _recruitmentService.StopReceiveRecruitableNations();
                        }
                    }
                    else
                    {
                        await ReplyAsync($"There is already a /rn command running. Try again later.");
                    }
                }
                else
                {
                    await ReplyAsync(AppSettings._permissionDeniedResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured"));
                await ReplyAsync($"Something went wrong. Sorry :( ");
            }
        }

        [Command("rns"), Summary("Returns the status of an /rn command")]
        public async Task DoGetRNStatusAsync()
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.RNSCommand);
            try
            {
                await ReplyAsync(_recruitmentService.GetRNStatus());
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured"));
                await ReplyAsync($"Something went wrong :( ");
            }
        }

        [Command("rstat"), Summary("Returns statistics to determine the effectiveness of recruitment")]
        public async Task DoGetRecruitmentStatsAsync()
        {
            var watch = new Stopwatch();
            watch.Start();
            if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
            {
                await _recruitmentService.UpdateRecruitmentStatsAsync();
                var builder = new EmbedBuilder();
                builder.WithTitle($"Recruitment statistics:");
                builder.WithDescription($"-- DataSource DB : Last updated just now --{Environment.NewLine}" +
                                        $"Sent (API): {_recruitmentService.ApiSent}{Environment.NewLine}" +
                                        $"Pending (API): {_recruitmentService.ApiPending}{Environment.NewLine}" +
                                        $"Failed (API): {_recruitmentService.ApiFailed}{Environment.NewLine}" +
                                        $"Skipped (API) : {_recruitmentService.ApiSkipped}{Environment.NewLine}" +
                                        $"Reserved (Manual): {_recruitmentService.ManualReserved}{Environment.NewLine}{Environment.NewLine}");
                builder.WithFooter(DiscordBotService.FooterString);

                await ReplyAsync(embed: builder.Build());
            }
            else
            {
                await ReplyAsync(AppSettings._permissionDeniedResponse);
            }
            watch.Stop();
            Console.WriteLine(watch.Elapsed);
        }
    }
}