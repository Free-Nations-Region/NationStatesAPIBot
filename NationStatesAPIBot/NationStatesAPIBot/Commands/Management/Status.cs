using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NationStatesAPIBot.Services;

namespace NationStatesAPIBot.Commands.Management
{
    public static class PingPongStats
    {
        public static int Pong { get; set; } = 1;
        public static int Ping { get; set; } = 1;
    }

    public class Status : ModuleBase<SocketCommandContext>
    {
        private readonly AppSettings _config;
        private readonly Random _random;

        public Status(IOptions<AppSettings> config)
        {
            _config = config.Value;
            _random = new Random();
        }

        [Command("status"), Summary("Returns some status information.")]
        public async Task GetStatusAsync()
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
                    Value = Context.Guild != null ? Context.Guild.Users.Count : 0
                },
                new EmbedFieldBuilder()
                {
                    Name = "Uptime",
                    Value = $"{uptime.Days} Days {uptime.Hours} Hours {uptime.Minutes} Minutes"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Dump Data",
                    Value = $"Available: {DumpDataService.DataAvailable}; Updating: {DumpDataService.IsUpdating}"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Last Dump Data Update",
                    Value = $"{(DumpDataService.LastDumpUpdateTimeUtc == DateTime.UnixEpoch || (!DumpDataService.DataAvailable && DumpDataService.IsUpdating)?"Updating":DateTime.UtcNow.Subtract(DumpDataService.LastDumpUpdateTimeUtc).ToString("h'h 'm'm 's's'") + " ago")}"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Recruitment",
                    Value = RecruitmentService.RecruitmentStatus
                },
                new EmbedFieldBuilder()
                {
                    Name = "Pool Status",
                    Value = RecruitmentService.PoolStatus
                }
            });
            await ReplyAsync(embed: builder.Build());
        }

        [Command("ping")]
        public async Task DoPingAsync()
        {
            if (_random.Next(1, 101) < 6)
            {
                await ReplyAsync("HA! Ponged! <:dab:566238284989202433>");
                PingPongStats.Ping = 1;
            }
            else if (_random.Next(1, 101) < 51 && PingPongStats.Ping % 20 == 0)
            {
                PingPongStats.Ping = 1;
                await ReplyAsync("<:pinged:534361662795546624> <:angrythonker:554677045322579969> <:Ban:570405775017508864>");
            }
            else
            {
                await ReplyAsync($"Pong ! - {PingPongStats.Ping}");
                PingPongStats.Ping += 1;
            }
        }

        [Command("pong")]
        public async Task DoPongAsync()
        {
            if (_random.Next(1, 101) < 6)
            {
                PingPongStats.Pong = 1;
                await ReplyAsync("<:amusing_2:695739982899576842> HA! You just got pinged! <:Amusing:685604446159503408>");
            }
            else if (_random.Next(1, 101) < 51 && PingPongStats.Pong % 20 == 0)
            {
                PingPongStats.Pong = 1;
                await ReplyAsync("<:angryping:554676847766667294> <:spam:534360406496509952>");
            }
            else
            {
                await ReplyAsync($"Ping ! - {PingPongStats.Pong}");
                PingPongStats.Pong += 1;
            }
        }
    }
}