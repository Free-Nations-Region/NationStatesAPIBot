using Discord.Commands;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NationStatesAPIBot.Interfaces;
using Microsoft.Extensions.Logging;
using NationStatesAPIBot.Services;

namespace NationStatesAPIBot.Commands.Management
{
    public class Recruitment : ModuleBase<SocketCommandContext>
    {
        readonly IPermissionManager _permManager;
        readonly ILogger<Recruitment> _logger;
        readonly RecruitmentService _recruitmentService;

        static readonly string actionQueued = $"Your action was queued successfully. Please be patient this may take a moment.";

        public Recruitment(IPermissionManager permissionManager, ILogger<Recruitment> logger, RecruitmentService recruitmentService)
        {
            _permManager = permissionManager ?? throw new ArgumentNullException(nameof(permissionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _recruitmentService = recruitmentService;
        }

        [Command("startRecruitment"), Summary("Starts the recruitment process")]
        public async Task DoStartRecruitment()
        {
            if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
            {
                _recruitmentService.StartRecruitment();
                await ReplyAsync("Not ready yet - Recruitment Process started.");
            }
            else
            {
                await ReplyAsync(AppSettings.PERMISSION_DENIED_RESPONSE);
            }
        }

        [Command("stopRecruitment"), Summary("Stops the recruitment process")]
        public async Task DoStopRecruitment()
        {
            if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
            {
                _recruitmentService.StopRecruitment();
                await ReplyAsync("Not ready yet - Recruitment Process stopped.");
            }
            else
            {
                await ReplyAsync(AppSettings.PERMISSION_DENIED_RESPONSE);
            }
        }

        [Command("rn"), Summary("Returns a list of nations which would receive an recruitment telegram")]
        public async Task DoGetRecruitableNations([Remainder, Summary("Number of nations to be returned")]int number)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.RNCommand);
            List<Nation> returnNations = new List<Nation>();
            try
            {
                if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
                {
                    if (!_recruitmentService.IsReceivingRecruitableNation)
                    {
                        if (number <= 120)
                        {
                            var currentRN = new RNStatus
                            {
                                IssuedBy = Context.User.Username,
                                FinalCount = number,
                                StartedAt = DateTimeOffset.UtcNow,
                                AvgTimePerFoundNation = TimeSpan.FromSeconds(2)
                            };
                            _recruitmentService.StartReceiveRecruitableNations(currentRN);
                            await ReplyAsync($"{actionQueued}{Environment.NewLine}{Environment.NewLine}You can request the status of this command using /rns. Finish expected in approx. (mm:ss): {currentRN.ExpectedIn().ToString(@"mm\:ss")}");
                            returnNations = await _recruitmentService.GetRecruitableNations(number);
                            foreach (var nation in returnNations)
                            {
                                await _recruitmentService.SetNationStatusToAsync(nation, "reserved_manual");
                            }
                            StringBuilder builder = new StringBuilder();
                            builder.AppendLine("-----");
                            var firstReplyStart = $"<@{Context.User.Id}> Your action just finished.{Environment.NewLine}Changed status of {number} nations from 'pending' to 'reserved_manual'.{Environment.NewLine}Recruitable Nations are (each segment for 1 telegram):{Environment.NewLine}";
                            int replyCount = (number / 40) + (number % 40 != 0 ? 1 : 0);

                            int currentReply = 1;
                            for (int i = 1; i <= returnNations.Count; i++)
                            {

                                var nation = returnNations[i - 1];
                                if (i % 8 == 0)
                                {
                                    builder.AppendLine($"{nation.Name}");
                                    builder.AppendLine("-----");
                                }
                                else
                                {
                                    builder.Append($"{nation.Name}, ");
                                }
                                if (i % 40 == 0)
                                {
                                    if (i / 40 == 1)
                                    {
                                        await ReplyAsync($"{firstReplyStart} Reply {currentReply}/{replyCount}{Environment.NewLine}{builder.ToString()}");
                                    }
                                    else
                                    {
                                        await ReplyAsync($"Reply {currentReply}/{replyCount}{Environment.NewLine}{builder.ToString()}");
                                    }
                                    builder.Clear();
                                    currentReply++;
                                }
                            }
                            if (returnNations.Count < 40)
                            {
                                await ReplyAsync($"{firstReplyStart}{builder.ToString()}");
                            }
                            else
                            {
                                if (number % 40 != 0)
                                {
                                    await ReplyAsync($"Reply {currentReply}/{replyCount}{Environment.NewLine}{builder.ToString()}");
                                }
                            }
                        }
                        else
                        {
                            await ReplyAsync($"{number} exceeds the maximum of 120 Nations (15 Telegrams a 8 recipients) to be returned.");
                        }
                    }
                    else
                    {
                        await ReplyAsync($"There is already a /rn command running. Try again later.");
                    }
                }
                else
                {
                    await ReplyAsync(AppSettings.PERMISSION_DENIED_RESPONSE);
                }
            }
            catch (Exception ex)
            {

                _logger.LogCritical(id, LogMessageBuilder.Build(id, "An critical error occured"), ex);
                await ReplyAsync($"Something went wrong :( ");
                foreach (var nation in returnNations)
                {
                    await _recruitmentService.SetNationStatusToAsync(nation, "pending");
                }
            }
            finally
            {
                _recruitmentService.StopReceiveRecruitableNations();
            }
        }

        [Command("rns"), Summary("Returns the status of an /rn command")]
        public async Task DoGetRNStatus()
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
                    await ReplyAsync(AppSettings.PERMISSION_DENIED_RESPONSE);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, LogMessageBuilder.Build(id, "An critical error occured"), ex);
                await ReplyAsync($"Something went wrong :( ");
            }
        }
    }
}
