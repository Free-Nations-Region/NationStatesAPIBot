using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NationStatesAPIBot
{
    public static class ActionManager
    {
        public const int API_VERSION = 9;
        public const long API_REQUEST_INTERVAL = 6000000; //0,6 s
        public const long SEND_NON_RECRUITMENTTELEGRAM_INTERVAL = 300000000; //30 s
        public const long SEND_RECRUITMENTTELEGRAM_INTERVAL = 1800000000; //3 m 1800000000
        public const long REQUEST_NEW_NATIONS_INTERVAL = 18000000000; //30 m 36000000000
        public const long REQUEST_REJECTED_NATIONS_INTERVAL = 432000000000; //12 h 432000000000
        /// <summary>
        /// Property that indicates if the bot was initialized and if config were loaded.
        /// </summary>
        public static bool Initialized { get; private set; }
        /// <summary>
        /// The clientKey required for sending telegrams
        /// </summary>
        internal static string NationStatesClientKey { get; private set; }
        /// <summary>
        /// The telegramId that specifies which (recruitment-)telegram should be send during recruitment progress.
        /// </summary>
        internal static string NationStatesRecruitmentTelegramID { get; private set; }
        /// <summary>
        /// The secretKey required for sending telegrams
        /// </summary>
        internal static string NationStatesSecretKey { get; private set; }
        /// <summary>
        /// The contact information to be added to the user agent. Could be a nation name or a email address or something like that.
        /// </summary>
        internal static string ContactInformation { get; private set; }
        /// <summary>
        /// The user agent to be added to each api request so that nation states can identify us.
        /// </summary>
        internal static string NationStatesAPIUserAgent { get; private set; }
        /// <summary>
        /// The controller class instance for performing NationStates API Calls
        /// </summary>
        internal static NationStatesApiController NationStatesApiController { get; private set; }
        /// <summary>
        /// The discord bot token required for login.
        /// </summary>
        internal static string DiscordBotLoginToken { get; private set; }
        /// <summary>
        /// The Logger Instance to be used for logging purposes
        /// </summary>
        internal static Logger LoggerInstance { get; private set; }
        /// <summary>
        /// The discord user id of the main admin of the bot. Used for checking permissions on admin only commands.
        /// </summary>
        internal static string BotAdminDiscordUserId { get; private set; }

        private static DiscordSocketClient discordClient { get; set; }
        private static CommandService commands;
        private static IServiceProvider services;
        private static readonly string source = "ActionManager";
        public static bool Running { get; private set; } = false;
        /// <summary>
        /// Intializes the ActionManager and the bot during StartUp
        /// </summary>
        public static async Task StartUp()
        {
            LoggerInstance = new Logger();
            await LoadConfig();
            await SetupDiscordBot();
            NationStatesApiController = new NationStatesApiController();
            Running = true;
        }

        public static async Task Shutdown()
        {
            await LoggerInstance.LogAsync(LogSeverity.Info, source, "Shutdown requested.");
            if (NationStatesApiController.isRecruiting)
            {
                await NationStatesApiController.StopRecruitingAsync();
            }
            await LoggerInstance.LogAsync(LogSeverity.Info, source, "Going offline.");
            await discordClient.SetStatusAsync(UserStatus.Offline);
            await discordClient.StopAsync();
            Running = false;
            Environment.Exit(0);
        }
        
        private static async Task LoadConfig()
        {
            if (File.Exists("keys.config"))
            {
                var content = await File.ReadAllLinesAsync("keys.config");
                var lines = content.ToList();
                if (lines.Exists(cl => cl.StartsWith("clientKey=")) && lines.Exists(t => t.StartsWith("telegramId=")) &&
                   lines.Exists(s => s.StartsWith("secretKey=")) && lines.Exists(c => c.StartsWith("contact=")) &&
                   lines.Exists(s => s.StartsWith("botLoginToken=")) && lines.Exists(s => s.StartsWith("botAdminUser=")))
                {
                    NationStatesClientKey = lines.Find(l => l.StartsWith("clientKey=")).Split("=")[1];
                    NationStatesRecruitmentTelegramID = lines.Find(l => l.StartsWith("telegramId=")).Split("=")[1];
                    NationStatesSecretKey = lines.Find(l => l.StartsWith("secretKey=")).Split("=")[1];
                    ContactInformation = lines.Find(c => c.StartsWith("contact=")).Split("=")[1];
                    DiscordBotLoginToken = lines.Find(c => c.StartsWith("botLoginToken=")).Split("=")[1];
                    BotAdminDiscordUserId = lines.Find(c => c.StartsWith("botAdminUser=")).Split("=")[1];
                    if (lines.Exists(c => c.StartsWith("logLevel=")))
                    {
                        if (int.TryParse(lines.Find(c => c.StartsWith("logLevel=")).Split("=")[1], out int value))
                        {
                            LoggerInstance.SeverityThreshold = (LogSeverity)value;
                        }
                    }
                    NationStatesAPIUserAgent = $"NationStatesAPIBot (https://github.com/drehtisch/NationStatesAPIBot) {Program.versionString} contact: {ContactInformation}";
                }
                else
                {
                    throw new InvalidDataException("Not all required values where specified. Please refer to documentation for information about how to configure properly.");
                }
            }
            else
            {
                throw new FileNotFoundException("The 'keys.config' file could not be found.");
            }
        }
        
        private static async Task SetupDiscordBot()
        {
            discordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LoggerInstance.SeverityThreshold
            });

            commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LoggerInstance.SeverityThreshold,
                SeparatorChar = ' ',
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async
            });

            services = new ServiceCollection()
                .BuildServiceProvider();
            discordClient.Connected += DiscordClient_Connected;
            discordClient.Disconnected += DiscordClient_Disconnected;
            discordClient.MessageReceived += DiscordClient_MessageReceived;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            discordClient.Ready += DiscordClient_Ready;
            discordClient.Log += DiscordClient_Log;
            await discordClient.LoginAsync(TokenType.Bot, DiscordBotLoginToken);
            await discordClient.StartAsync();
        }

        private static async Task DiscordClient_Log(LogMessage arg)
        {
            await LoggerInstance.LogAsync(arg.Severity, arg.Source, arg.Message);
        }

        

        private static async Task DiscordClient_Ready()
        {
            await SetClientAction("Lections of BotFather", ActivityType.Listening);
        }

        private static async Task DiscordClient_MessageReceived(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(discordClient, message);

            if (string.IsNullOrWhiteSpace(context.Message.Content) || context.User.IsBot) return;

            int argPos = 0;
            if (!(message.HasCharPrefix('/', ref argPos) || message.HasMentionPrefix(discordClient.CurrentUser, ref argPos))) return;


            var Result = await commands.ExecuteAsync(context, argPos, services);
        }

        private static async Task DiscordClient_Disconnected(Exception arg)
        {
            await LoggerInstance.LogAsync(LogSeverity.Info, source, "Disconnected from Discord");
        }

        private static async Task DiscordClient_Connected()
        {
            await LoggerInstance.LogAsync(LogSeverity.Info, source, "Connected to Discord");
        }

        /// <summary>
        /// Does set the Bot action displayed in the discord
        /// </summary>
        /// <param name="description">The text to be displayed</param>
        /// <param name="activityType">The discord actionType</param>
        public static async Task SetClientAction(string description, ActivityType activityType)
        {
            await discordClient.SetGameAsync(description, null, activityType);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isScheduledAction"></param>
        /// <returns></returns>
        public static bool IsNationStatesApiActionAllowed(NationStatesApiRequestType type, bool isScheduledAction)
        {
            throw new NotImplementedException("Not implemented yet");
        }
    }
}
