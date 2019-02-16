using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Managers
{
    public static class ActionManager
    {
        public const int API_VERSION = 9;
        public const long API_REQUEST_INTERVAL = 6000000; //0,6 s
        public const long SEND_NON_RECRUITMENTTELEGRAM_INTERVAL = 300000000; //30 s
        public const long SEND_RECRUITMENTTELEGRAM_INTERVAL = 1800000000; //3 m 1800000000
        public const long REQUEST_NEW_NATIONS_INTERVAL = 18000000000; //30 m 36000000000
        public const long REQUEST_REGION_NATIONS_INTERVAL = 432000000000; //12 h 432000000000
        public const string BOT_ADMIN_TERM = "BotFather"; //Change to something else if you don't like the term
        public static readonly string PERMISSION_DENIED_RESPONSE = $"Sorry, but i can't do that for you. Reason: Permission denied. Contact {BOT_ADMIN_TERM} if you think that is an issue. Use /callHelp and i will contact {BOT_ADMIN_TERM} for you. (Do not overuse you could be ignored.)";
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
        internal static string NationStatesRecruitmentTGSecretKey { get; private set; }
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
            await InitDb();
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

        private static async Task InitDb()
        {
            using (var dbContext = new BotDbContext())
            {
                var executeCommandPermission = new Entities.Permission() { Name = "ExecuteCommands", Description = "Determines if a User or Role can execute bot commands." };
                if (dbContext.Permissions.Count() == 0)
                {
                    await LoggerInstance.LogAsync(LogSeverity.Debug, source, "Initializing permissions.");
                    await dbContext.Permissions.AddAsync(executeCommandPermission);
                    await dbContext.Permissions.AddAsync(new Permission() { Name = "Shutdown", Description = "Determines if a User or Role is allowed to turn the bot off." });
                    await dbContext.Permissions.AddAsync(new Permission() { Name = "AccessPending", Description = "Determines if a User or Role is allowed to access or refresh the pending list of nations for the recruitment process." });
                    await dbContext.Permissions.AddAsync(new Permission() { Name = "ManagePermissions", Description = "Determines if a User or Role is allowed to read, grant and revoke permissions to Users and Roles." });
                    await dbContext.Permissions.AddAsync(new Permission() { Name = "ManageRoles", Description = "Determines if a User or Role is allowed to read, assign and remove Roles from Users." });
                    await dbContext.Permissions.AddAsync(new Permission() { Name = "ManageRecruiting", Description = "Determines if a User or Role is allowed to start or stop the recruitment process." });
                    await dbContext.SaveChangesAsync();
                }
                if(dbContext.Roles.Count() == 0)
                {
                    await LoggerInstance.LogAsync(LogSeverity.Debug, source, "Initializing roles.");
                    var role = new Role() { Description = "Default-User" };
                    await dbContext.Roles.AddAsync(role);
                    await dbContext.SaveChangesAsync();
                    role.RolePermissions = new List<RolePermissions>
                    {
                        new RolePermissions() { Permission = executeCommandPermission, PermissionId = executeCommandPermission.Id, Role = role, RoleId = role.Id }
                    };
                    dbContext.Update(role);
                    await dbContext.SaveChangesAsync();
                }
            }
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
                    NationStatesRecruitmentTGSecretKey = lines.Find(l => l.StartsWith("secretKey=")).Split("=")[1];
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
            await SetClientAction($"Lections of {BOT_ADMIN_TERM}", ActivityType.Listening);
        }

        private static async Task DiscordClient_MessageReceived(SocketMessage arg)
        {
            try
            {
                var message = arg as SocketUserMessage;
                var context = new SocketCommandContext(discordClient, message);

                if (string.IsNullOrWhiteSpace(context.Message.Content) || context.User.IsBot) return;

                int argPos = 0;
                if (!(message.HasCharPrefix('/', ref argPos) || message.HasMentionPrefix(discordClient.CurrentUser, ref argPos) || PermissionManager.IsAllowed(PermissionType.ExecuteCommands, context.User))) return;
                var Result = await commands.ExecuteAsync(context, argPos, services);
            }
            catch (Exception ex)
            {
                await LoggerInstance.LogAsync(LogSeverity.Critical, $"{source}-MessageReceived", ex.ToString());
            }
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
        public static bool IsNationStatesApiActionReady(NationStatesApiRequestType type, bool isScheduledAction)
        {
            if (type == NationStatesApiRequestType.GetNationsFromRegion)
            {
                return DateTime.Now.Ticks - NationStatesApiController.lastAutomaticRegionNationsRequest.Ticks > (isScheduledAction ? REQUEST_REGION_NATIONS_INTERVAL : API_REQUEST_INTERVAL);
            }
            else if (type == NationStatesApiRequestType.GetNewNations)
            {
                return DateTime.Now.Ticks - NationStatesApiController.lastAutomaticNewNationsRequest.Ticks > (isScheduledAction ? REQUEST_NEW_NATIONS_INTERVAL : API_REQUEST_INTERVAL);
            }
            else if (type == NationStatesApiRequestType.SendTelegram)
            {
                return DateTime.Now.Ticks - NationStatesApiController.lastTelegramSending.Ticks > SEND_NON_RECRUITMENTTELEGRAM_INTERVAL;
            }
            else if (type == NationStatesApiRequestType.SendRecruitmentTelegram)
            {
                return DateTime.Now.Ticks - NationStatesApiController.lastTelegramSending.Ticks > SEND_RECRUITMENTTELEGRAM_INTERVAL;
            }
            else
            {
                return DateTime.Now.Ticks - NationStatesApiController.lastAPIRequest.Ticks > API_REQUEST_INTERVAL;
            }

        }
    }
}
