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
                await ReplyAsync("Recruitment Process started.");
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
                await ReplyAsync("Recruitment Process stopped.");
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
                    if (!_recruitmentService.IsReceivingRecruitableNations)
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
                            _logger.LogInformation(id, LogMessageBuilder.Build(id, $"{number} recruitable nations requested."));
                            returnNations = await _recruitmentService.GetRecruitableNationsAsync(number);
                            foreach (var nation in returnNations)
                            {
                                await NationManager.SetNationStatusToAsync(nation, "reserved_manual");
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

                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "An critical error occured"));
                await ReplyAsync($"Something went wrong :( ");
                foreach (var nation in returnNations)
                {
                    await NationManager.SetNationStatusToAsync(nation, "pending");
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
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "An critical error occured"));
                await ReplyAsync($"Something went wrong :( ");
            }
        }
        
        [Command("rstat"), Summary("Returns statistics to determine the effectiveness of recruitment")]
        public async Task DoGetRecruitmentStats()
        {
            if (await _permManager.IsAllowedAsync(PermissionType.ManageRecruitment, Context.User))
            {
                var builder = new EmbedBuilder();
                builder.WithTitle($"Recruitment statistics:");
                builder.WithDescription($"Sent (API): {_recruitmentService.ApiSent}{Environment.NewLine}" +
                                        $"Pending (API): {_recruitmentService.ApiPending}{Environment.NewLine}" +
                                        $"Failed (API): {_recruitmentService.ApiFailed}{Environment.NewLine}" +
                                        $"Recruited (API): {_recruitmentService.ApiRecruited} ({_recruitmentService.ApiRatio.ToString(new CultureInfo("en-US"))}%){Environment.NewLine}" +
                                        $"Skipped : {_recruitmentService.ApiSkipped}{Environment.NewLine}" +
                                        $"Reserved (Manual): {_recruitmentService.ManualReserved}{Environment.NewLine}" +
                                        $"Recruited (Manual): {_recruitmentService.ManualRecruited} ({_recruitmentService.ManualRatio.ToString(new CultureInfo("en-US"))}%){Environment.NewLine}" +
                                        $"{Environment.NewLine}" +
                                        $"Recruited Today: A: {_recruitmentService.RecruitedTodayA}, M: {_recruitmentService.RecruitedTodayM}{Environment.NewLine}" +
                                        $"Recruited Yesterday: A: {_recruitmentService.RecruitedYesterdayA}, M: {_recruitmentService.RecruitedYesterdayM}{Environment.NewLine}" +
                                        $"Recruited Last Week: A: {_recruitmentService.RecruitedLastWeekA}, M: {_recruitmentService.RecruitedLastWeekM}{Environment.NewLine}" +
                                        $"Recruited Last Week (Avg/D): A: {_recruitmentService.RecruitedLastWeekAvgDA.ToString("0.00", new CultureInfo("en-US"))}, M: {_recruitmentService.RecruitedLastWeekAvgDM.ToString("0.00", new CultureInfo("en-US"))}{Environment.NewLine}" +
                                        $"Recruited Last Month: A: {_recruitmentService.RecruitedLastMonthA}, M: {_recruitmentService.RecruitedLastMonthM}{Environment.NewLine}" +
                                        $"Recruited Last Month (Avg/D): A: {_recruitmentService.RecruitedLastMonthAvgDA.ToString("0.00", new CultureInfo("en-US"))}, M: {_recruitmentService.RecruitedLastMonthAvgDM.ToString("0.00", new CultureInfo("en-US"))}{Environment.NewLine}{Environment.NewLine}" +
                                        $"Recruits which CTE'd or left the region are excluded.");
                builder.WithFooter($"NationStatesApiBot {AppSettings.VERSION} by drehtisch");
                
                await ReplyAsync(embed: builder.Build());
            }
            else
            {
                await ReplyAsync(AppSettings.PERMISSION_DENIED_RESPONSE);
            }
        }
    }
}
