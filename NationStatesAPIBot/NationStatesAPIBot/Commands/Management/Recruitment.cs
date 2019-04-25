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
                ActionManager.NationStatesApiController.StartRecruitingAsync();
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
                ActionManager.NationStatesApiController.StopRecruitingAsync();
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
            if (PermissionManager.IsAllowed(PermissionType.ManageRecruitment, Context.User))
            {
                if (!ActionManager.receivingRecruitableNation)
                {
                    if (number <= 120)
                    {
                        ActionManager.receivingRecruitableNation = true;
                        await ReplyAsync(actionQueued);
                        List<Nation> returnNations = await NationStatesApiController.GetRecruitableNations(number);
                        foreach (var nation in returnNations)
                        {
                            await ActionManager.NationStatesApiController.SetNationStatusToAsync(nation, "reserved_manual");
                        }
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("-----");
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
                        }
                        await ReplyAsync($"<@{Context.User.Id}> Your action just finished.{Environment.NewLine}Changed status of {number} nations from 'pending' to 'reserved_manual'.{Environment.NewLine}Recruitable Nations are (each segment for 1 telegram):{Environment.NewLine}{builder.ToString()}");
                    }
                    else
                    {
                        await ReplyAsync($"{number} exceeds the maximum of 120 Nations (15 Telegrams a 8 recipients) to be returned.");
                    }
                    ActionManager.receivingRecruitableNation = false;
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


    }
}
