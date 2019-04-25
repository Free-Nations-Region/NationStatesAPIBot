using Discord;
using Discord.Commands;
using NationStatesAPIBot.Managers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Commands.Management
{
    public class Status : ModuleBase<SocketCommandContext>
    {
        [Command("status"), Summary("Returns some status information.")]
        public async Task GetStatus()
        {
            var builder = new EmbedBuilder();
            builder.WithTitle("Bot Status");
            TimeSpan uptime = DateTime.UtcNow.Subtract(ActionManager.StartUpTime);
            TimeSpan recruitingTime = DateTime.UtcNow.Subtract(ActionManager.NationStatesApiController.RecruitmentStarttime);
            builder.WithFields(new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder()
                {
                    Name = "Version:",
                    Value = Program.versionString
                },
                new EmbedFieldBuilder()
                {
                    Name = "Configuration:",
                    Value = ActionManager.Configuration
                },
                new EmbedFieldBuilder()
                {
                    Name = "Uptime:",
                    Value = $"{uptime.Days} Days {uptime.Hours} Hours {uptime.Minutes} Minutes"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Recruitment Running:",
                    Value = ActionManager.NationStatesApiController.IsRecruiting
                },
                new EmbedFieldBuilder()
                {
                    Name = "Recruitment Uptime:",
                    Value = ActionManager.NationStatesApiController.IsRecruiting?
                    $"{recruitingTime.Days} Days {recruitingTime.Hours} Hours {recruitingTime.Minutes} Minutes":
                    "-"
                }
            });
            await ReplyAsync("", false, builder.Build());
        }
        [Command("ping"), Summary("Does reply Pong on receiving Ping")]
        public async Task DoPing()
        {
            await ReplyAsync("Pong !");
        }
    }
}
