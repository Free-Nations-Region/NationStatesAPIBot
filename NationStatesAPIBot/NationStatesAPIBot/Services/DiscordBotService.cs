using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Interfaces;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using NationStatesAPIBot.Types;
using NationStatesAPIBot.Manager;
using NationStatesAPIBot.Managers;

namespace NationStatesAPIBot.Services
{
    public class DiscordBotService : IBotService
    {
        private readonly ILogger<DiscordBotService> _logger;
        private readonly AppSettings _config;
        private DiscordSocketClient DiscordClient;
        private CommandService commandService;
        private IPermissionManager _permManager;

        public bool IsRunning { get; private set; }

        public DiscordBotService(ILogger<DiscordBotService> logger, IOptions<AppSettings> config, IPermissionManager permissionManager)
        {
            _logger = logger;
            _config = config.Value;
            _permManager = permissionManager;
        }

        public async Task<bool> IsRelevantAsync(object message, object user)
        {
            if (message is SocketUserMessage socketMsg && user is SocketUser socketUser)
            {
                string userId = socketUser.Id.ToString();
                if (!UserManager.IsUserInDb(userId).Result)
                {
                    await UserManager.AddUserToDbAsync(userId);
                }
                var value = !string.IsNullOrWhiteSpace(socketMsg.Content) &&
                    !socketUser.IsBot && 
                    socketMsg.Content.StartsWith(_config.SeperatorChar) &&
                    await _permManager.IsAllowedAsync(PermissionType.ExecuteCommands, socketUser);
                return _config.Configuration == "development" ?
                    await _permManager.IsBotAdminAsync(socketUser) && value 
                    : value;
            }
            return await Task.FromResult(false);
        }

        public async Task ProcessMessageAsync(object message)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.UserMessage);
            try
            {
                if (message is SocketUserMessage socketMsg)
                {
                    var context = new SocketCommandContext(DiscordClient, socketMsg);
                    _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{socketMsg.Author.Username} in {socketMsg.Channel.Name}: {socketMsg.Content}"));
                    if (await IsRelevantAsync(message, context.User))
                    {
                        await commandService.ExecuteAsync(context, 1, Program.ServiceProvider);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
            }
        }

        public async Task RunAsync()
        {
            _logger.LogInformation($"--- DiscordBotService started ---");
            NationManager.Initialize(_config);
            UserManager.Initialize(_config);
            DiscordClient = new DiscordSocketClient();
            commandService = new CommandService(new CommandServiceConfig
            {
                SeparatorChar = _config.SeperatorChar,
                DefaultRunMode = RunMode.Async,
                CaseSensitiveCommands = false
            });
            SetUpDiscordEvents();
            await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), Program.ServiceProvider);
            await DiscordClient.LoginAsync(TokenType.Bot, _config.DiscordBotLoginToken);
            await DiscordClient.StartAsync();
            IsRunning = true;

        }

        private void SetUpDiscordEvents()
        {
            DiscordClient.Connected += DiscordClient_Connected;
            DiscordClient.Disconnected += DiscordClient_Disconnected;
            DiscordClient.MessageReceived += DiscordClient_MessageReceived;
            DiscordClient.Log += DiscordClient_Log;
            DiscordClient.LoggedIn += DiscordClient_LoggedIn;
            DiscordClient.LoggedOut += DiscordClient_LoggedOut;
            DiscordClient.Ready += DiscordClient_Ready;
            DiscordClient.UserBanned += DiscordClient_UserBanned;
            DiscordClient.UserJoined += DiscordClient_UserJoined;
            DiscordClient.UserLeft += DiscordClient_UserLeft;
        }

        private Task DiscordClient_UserLeft(SocketGuildUser arg)
        {
            _logger.LogInformation($"User {arg.Username}#{arg.Discriminator} left the server.");
            return Task.CompletedTask;
        }

        private Task DiscordClient_UserJoined(SocketGuildUser arg)
        {
            _logger.LogInformation($"User {arg.Username}{arg.Discriminator} joined the server.");
            return Task.CompletedTask;
        }

        private async Task DiscordClient_UserBanned(SocketUser arg1, SocketGuild arg2)
        {
            _logger.LogInformation($"User {arg1.Username}{arg1.Discriminator} was banned from the {arg2.Name} server.");
            await UserManager.RemoveUserFromDbAsync(arg1.Id.ToString());
        }

        private Task DiscordClient_Ready()
        {
            _logger.LogInformation("--- Discord Client Ready ---");
            return Task.CompletedTask;
        }

        private Task DiscordClient_LoggedOut()
        {
            _logger.LogInformation("--- Bot logged out ---");
            return Task.CompletedTask;
        }

        private Task DiscordClient_LoggedIn()
        {
            _logger.LogInformation("--- Bot logged in ---");
            return Task.CompletedTask;
        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.DiscordLogEvent);
            string message = LogMessageBuilder.Build(id, $"[{arg.Source}] {arg.Message}");
            switch (arg.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(id, message);
                    break;
                case LogSeverity.Error:
                    _logger.LogError(id, message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(id, message);
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation(id, message);
                    break;
                default:
                    _logger.LogDebug(id, $"Severity: {arg.Severity.ToString()} {message}");
                    break;
            }
            return Task.CompletedTask;
        }

        private async Task DiscordClient_MessageReceived(SocketMessage arg)
        {
            await ProcessMessageAsync(arg);
        }

        private Task DiscordClient_Disconnected(Exception arg)
        {
            _logger.LogInformation(arg, "--- Disconnected from Discord ---");
            return Task.CompletedTask;
        }

        private Task DiscordClient_Connected()
        {
            _logger.LogInformation("--- Connected to Discord ---");
            return Task.CompletedTask;
        }

        public async Task ShutdownAsync()
        {
            await DiscordClient.LogoutAsync();
            await DiscordClient.StopAsync();
            IsRunning = false;
            Environment.Exit(0);
        }
    }
}
