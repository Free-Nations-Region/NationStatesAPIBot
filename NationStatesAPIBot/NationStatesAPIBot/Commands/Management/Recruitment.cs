using Discord.Commands;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Commands.Management
{
    public class Recruitment : ModuleBase<SocketCommandContext>
    {
        static readonly string actionQueued = $"Your action was queued successfully. Please be patient this may take a moment.";
        [Command("startRecruitment"), Summary("Starts the recruitment process")]
        public async Task DoStartRecruitment()
        {
            if (PermissionManager.IsAllowed(PermissionType.ManageRecruitment, Context.User))
            {
                ActionManager.NationStatesApiController.StartRecruiting();
                await ReplyAsync("Recruitment Process started.");
            }
            else
            {
                //TODO: Move permission denied into isAllowed
                await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            }
        }

        [Command("stopRecruitment"), Summary("Stops the recruitment process")]
        public async Task DoStopRecruitment()
        {
            if (PermissionManager.IsAllowed(PermissionType.ManageRecruitment, Context.User))
            {
                ActionManager.NationStatesApiController.StopRecruiting();
                await ReplyAsync("Recruitment Process stopped.");
            }
            else
            {
                await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            }
        }

        [Command("rn"), Summary("Returns a list of nations which would receive an recruitment telegram")]
        public async Task DoGetRecruitableNations([Remainder, Summary("Number of nations to be returned")]int number)
        {
            List<Nation> returnNations = new List<Nation>();
            try
            {
                if (PermissionManager.IsAllowed(PermissionType.ManageRecruitment, Context.User))
                {
                    if (!ActionManager.receivingRecruitableNation)
                    {
                        if (number <= 120)
                        {
                            ActionManager.receivingRecruitableNation = true;
                            await ReplyAsync(actionQueued);
                            returnNations = await NationStatesApiController.GetRecruitableNations(number);
                            foreach (var nation in returnNations)
                            {
                                await ActionManager.NationStatesApiController.SetNationStatusToAsync(nation, "reserved_manual");
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
                    await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
                }
            }
            catch (Exception ex)
            {
                NationStatesApiController.Log(Discord.LogSeverity.Critical, $"An critical error occured: {ex}");
                await ReplyAsync($"Something went wrong :( ");
                foreach(var nation in returnNations)
                {
                    await ActionManager.NationStatesApiController.SetNationStatusToAsync(nation, "pending");
                }
            }
            finally
            {
                ActionManager.receivingRecruitableNation = false;
            }
        }


    }
}
