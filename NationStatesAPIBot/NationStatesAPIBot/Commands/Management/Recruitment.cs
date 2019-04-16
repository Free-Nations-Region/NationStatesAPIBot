using Discord.Commands;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
using System.Collections.Generic;
using System.Linq;
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
            List<Nation> pendingNations = new List<Nation>();
            if (pendingNations.Count == 0)
            {
                pendingNations = ActionManager.NationStatesApiController.GetNationsByStatusName("pending");
            }
            var picked = pendingNations.Take(1);
            var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
        }
    }
}
