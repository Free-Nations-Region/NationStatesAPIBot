using Discord.Commands;
using NationStatesAPIBot.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NationStatesAPIBot.Commands.Management
{
    public class Shutdown : ModuleBase<SocketCommandContext>
    {
        [Command("shutdown"), Alias("stop"), Summary("Shuts down the bot")]
        public async Task DoShutdownAsync()
        {
            var permManager = Program.ServiceProvider.GetService<IPermissionManager>();
            if (await permManager.IsBotAdminAsync(Context.User))
            {
                await ReplyAsync("Shutting down. Bye Bye !");
                await Program.ServiceProvider.GetService<IBotService>().ShutdownAsync();
            }
        }
    }
}