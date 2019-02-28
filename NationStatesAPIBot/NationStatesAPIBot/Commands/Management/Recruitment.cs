using Discord.Commands;
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
        [Command("startRecruitment"), Summary("Starts the recruitment process")]
        public async Task DoStartRecruitment()
        {
            if (PermissionManager.IsAllowed(PermissionType.ManageRecruitment, Context.User))
            {
                await ActionManager.NationStatesApiController.StartRecruitingAsync();
                await ReplyAsync("Recruitment Process started.");
            }
            else
            {
                await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            }
        }

        [Command("stopRecruitment"), Summary("Stops the recruitment process")]
        public async Task DoStopRecruitment()
        {
            if (PermissionManager.IsAllowed(PermissionType.ManageRecruitment, Context.User))
            {
                await ActionManager.NationStatesApiController.StopRecruitingAsync();
                await ReplyAsync("Recruitment Process stopped.");
            }
            else
            {
                await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            }
        }
    }
}
