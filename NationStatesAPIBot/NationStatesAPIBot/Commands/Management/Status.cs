using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NationStatesAPIBot.Services;

namespace NationStatesAPIBot.Commands.Management
{
    public class Status : ModuleBase<SocketCommandContext>
    {
        private readonly AppSettings _config;
        public Status(IOptions<AppSettings> config)
        {
            _config = config.Value;
        }

        [Command("status"), Summary("Returns some status information.")]
        public async Task GetStatus()
        {
            var builder = new EmbedBuilder();
            builder.WithTitle("Bot Status");
            var configuration = "Production";
            var adminUser = Context.Client.GetUser(_config.DiscordBotAdminUser);
            var startTime = Program.StartTime;
            var uptime = DateTime.UtcNow.Subtract(startTime);
#if DEBUG
            configuration = "Development";
#endif
            builder.WithFields(new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder()
                {
                    Name = "Version:",
                    Value = AppSettings.VERSION
                },
                new EmbedFieldBuilder()
                {
                    Name = "Configuration:",
                    Value = configuration
                },
                new EmbedFieldBuilder()
                {
                    Name = $"{AppSettings.BOT_ADMIN_TERM} (Bot Admin/Developer)",
                    Value = $"{adminUser.Username}#{adminUser.Discriminator}"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Number of Users on this Server:",
                    Value = Context.Guild != null ? Context.Guild.Users.Count: 0
                },
                new EmbedFieldBuilder()
                {
                    Name = "Uptime",
                    Value = $"{uptime.Days} Days {uptime.Hours} Hours {uptime.Minutes} Minutes"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Recruitment",
                    Value = RecruitmentService.IsRecruiting?"Running":"Not Running"
                }
            });
            await ReplyAsync(embed: builder.Build());
        }
        [Command("ping"), Summary("Does reply Pong on receiving Ping")]
        public async Task DoPing()
        {
            await ReplyAsync("Pong !");
        }
    }
}
