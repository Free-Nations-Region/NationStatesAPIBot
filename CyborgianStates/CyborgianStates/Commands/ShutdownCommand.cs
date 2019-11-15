using CyborgianStates.Interfaces;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CyborgianStates.Commands
{
    public class ShutdownCommand : ModuleBase<SocketCommandContext>
    {
        [Command("shutdown"), Alias("stop"), Summary("Shuts down the bot")]
        public async Task DoShutdown()
        {
            var permManager = Program.ServiceProvider.GetService<IUserRepository>();
            if (await permManager.IsBotAdminAsync(Context.User.Id))
            {
                await ReplyAsync("Shutting down. Bye Bye !");
                await Program.ServiceProvider.GetService<IBotService>().ShutdownAsync();
            }
        }
    }
}
