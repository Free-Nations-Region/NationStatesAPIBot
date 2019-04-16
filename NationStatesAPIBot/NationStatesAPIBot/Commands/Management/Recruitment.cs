using Discord;
using Discord.Commands;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
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
                if (number <= 120)
                {
                    await ReplyAsync(actionQueued);
                    List<Nation> returnNations = new List<Nation>();
                    List<Nation> pendingNations = new List<Nation>();
                    if (pendingNations.Count == 0)
                    {
                        pendingNations = ActionManager.NationStatesApiController.GetNationsByStatusName("pending");
                    }
                    while (returnNations.Count < number)
                    {   
                        var picked = pendingNations.Take(1);
                        var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                        if (nation != null)
                        {
                            while (!await ActionManager.NationStatesApiController.CanReceiveRecruitmentTelegram(nation.Name))
                            {
                                pendingNations.Remove(nation);
                                await ActionManager.NationStatesApiController.SetNationStatusToSkippedAsync(nation);
                                picked = pendingNations.Take(1);
                                nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                                NationStatesApiController.Log(LogSeverity.Debug, "Recruitment", $"Nation: {nation.Name} would not receive this recruitment telegram and is therefore skipped.");
                            }
                            pendingNations.Remove(nation);
                            returnNations.Add(nation);
                            //To-Do: Set Nations to Manual
                        }
                    }
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine("-----");
                    for (int i = 1; i <= returnNations.Count; i++)
                    {
                        var nation = returnNations[i-1];
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
                    await ReplyAsync($"<@{Context.User.Id}> Your action just finished. Recruitable Nations are (each segment for 1 telegram):{Environment.NewLine}{builder.ToString()}");
                }
                else
                {
                    await ReplyAsync($"{number} exceeds the maximum of 120 Nations (15 Telegrams a 8 recipients) to be returned.");
                }
            }
            else
            {
                await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            }
        }
    }
}
