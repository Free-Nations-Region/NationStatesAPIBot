using Discord.Commands;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Commands.Management
{
    public class Shutdown : ModuleBase<SocketCommandContext>
    {
        [Command("shutdown"), Summary("Shuts down the bot")]
        public async Task DoShutdown()
        {
            if(PermissionManager.IsAllowed(PermissionType.ShutDown, Context.User))
            {
                await ReplyAsync("Shutting down. Bye Bye !");
                await ActionManager.Shutdown();
            }
            else
            {
                await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            }
        }
    }
}
