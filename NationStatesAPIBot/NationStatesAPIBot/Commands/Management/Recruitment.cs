﻿using Discord.Commands;
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
            await ReplyAsync("The /rn command needed to be disabled because of major issues with the manual recruitment system. Drehtisch will fix those issue asap. Sorry :(");

            //var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.RNCommand);
            //List<Nation> returnNations = new List<Nation>();
            //try
            //{
            //    if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
            //    {
            //        if (!_recruitmentService.IsReceivingRecruitableNations)
            //        {
            //            if (number <= 120)
            //            {
            //                var currentRN = new RNStatus
            //                {
            //                    IssuedBy = Context.User.Username,
            //                    FinalCount = number,
            //                    StartedAt = DateTimeOffset.UtcNow,
            //                    AvgTimePerFoundNation = TimeSpan.FromSeconds(2)
            //                };
            //                var channel = await Context.User.GetOrCreateDMChannelAsync();

            //                _recruitmentService.StartReceiveRecruitableNations(currentRN);
            //                await ReplyAsync($"{actionQueued}{Environment.NewLine}{Environment.NewLine}You can request the status of this command using /rns. Finish expected in approx. (mm:ss): {currentRN.ExpectedIn().ToString(@"mm\:ss")}");
            //                _logger.LogInformation(id, LogMessageBuilder.Build(id, $"{number} recruitable nations requested."));
            //                returnNations = await _recruitmentService.GetRecruitableNationsAsync(number, false);
            //                foreach (var nation in returnNations)
            //                {
            //                    await NationManager.SetNationStatusToAsync(nation, "reserved_manual");
            //                }
            //                StringBuilder builder = new StringBuilder();
            //                builder.AppendLine("-----");
            //                var firstReplyStart = $"<@{Context.User.Id}> Your action just finished.{Environment.NewLine}Changed status of {returnNations.Count} nations from 'pending' to 'reserved_manual'.{Environment.NewLine}Recruitable Nations are (each segment for 1 telegram):{Environment.NewLine}";
            //                int replyCount = (number / 40) + (number % 40 != 0 ? 1 : 0);

            //                int currentReply = 1;
            //                for (int i = 1; i <= returnNations.Count; i++)
            //                {
            //                    var nation = returnNations[i - 1];
            //                    if (i % 8 == 0)
            //                    {
            //                        builder.AppendLine($"{nation.Name}");
            //                        builder.AppendLine("-----");
            //                    }
            //                    else
            //                    {
            //                        builder.Append($"{nation.Name}, ");
            //                    }
            //                    if (i % 40 == 0)
            //                    {
            //                        if (i / 40 == 1)
            //                        {
            //                            await channel.SendMessageAsync($"{firstReplyStart} Reply {currentReply}/{replyCount}{Environment.NewLine}{builder.ToString()}");
            //                        }
            //                        else
            //                        {
            //                            await channel.SendMessageAsync($"Reply {currentReply}/{replyCount}{Environment.NewLine}{builder.ToString()}");
            //                        }
            //                        builder.Clear();
            //                        currentReply++;
            //                    }
            //                }
            //                if (returnNations.Count < 40)
            //                {
            //                    await channel.SendMessageAsync($"{firstReplyStart}{builder.ToString()}");
            //                }
            //                else
            //                {
            //                    if (number % 40 != 0)
            //                    {
            //                        await channel.SendMessageAsync($"Reply {currentReply}/{replyCount}{Environment.NewLine}{builder.ToString()}");
            //                    }
            //                }
            //                if(returnNations.Count < number)
            //                {
            //                    await ReplyAsync($"{Environment.NewLine}- - - - -{Environment.NewLine}WARNING: No more nations in pending nations pool.");
            //                }
            //            }
            //            else
            //            {
            //                await ReplyAsync($"{number} exceeds the maximum of 120 Nations (15 Telegrams a 8 recipients) to be returned.");
            //            }
            //        }
            //        else
            //        {
            //            await ReplyAsync($"There is already a /rn command running. Try again later.");
            //        }
            //    }
            //    else
            //    {
            //        await ReplyAsync(AppSettings.PERMISSION_DENIED_RESPONSE);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "An critical error occured"));
            //    await ReplyAsync($"Something went wrong :( ");
            //    foreach (var nation in returnNations)
            //    {
            //        await NationManager.SetNationStatusToAsync(nation, "pending");
            //    }
            //}
            //finally
            //{
            //    _recruitmentService.StopReceiveRecruitableNations();
            //}
        }

        [Command("rns"), Summary("Returns the status of an /rn command")]
        public async Task DoGetRNStatusAsync()
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.RNSCommand);
            try
            {
                if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
                {
                    await ReplyAsync(_recruitmentService.GetRNStatus());
                }
                else
                {
                    await ReplyAsync(AppSettings._permissionDeniedResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "An critical error occured"));
                await ReplyAsync($"Something went wrong :( ");
            }
        }

        [Command("rstat"), Summary("Returns statistics to determine the effectiveness of recruitment")]
        public async Task DoGetRecruitmentStatsAsync()
        {
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
                                        $"Reserved (Manual): {_recruitmentService.ManualReserved}{Environment.NewLine}{Environment.NewLine}" +
                                        $"-- DataSource Dump : Last updated {DateTime.UtcNow.Subtract(DumpDataService.LastDumpUpdateTimeUtc).ToString("h'h 'm'm 's's'")} ago --{Environment.NewLine}" +
                                        $"Recruited (API): {_recruitmentService.ApiRecruited} ({_recruitmentService.ApiRatio.ToString(_locale)}%){Environment.NewLine}" +
                                        $"Recruited (Manual): {_recruitmentService.ManualRecruited} ({_recruitmentService.ManualRatio.ToString(_locale)}%){Environment.NewLine}" +
                                        $"{Environment.NewLine}" +
                                        $"Recruits which CTE'd or left the region are excluded.");
                builder.WithFooter(DiscordBotService.FooterString);

                await ReplyAsync(embed: builder.Build());
            }
            else
            {
                await ReplyAsync(AppSettings._permissionDeniedResponse);
            }
        }
    }
}