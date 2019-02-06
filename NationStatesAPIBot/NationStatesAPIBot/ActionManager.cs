using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

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
        /// The DiscordSocketClient used for bot actions
        /// </summary>
        internal static DiscordSocketClient DiscordSocketClient { get; private set; }
        /// <summary>
        /// The Logger Instance to be used for logging purposes
        /// </summary>
        internal static Logger LoggerInstance { get; private set; }
        /// <summary>
        /// The discord user id of the main admin of the bot. Used for checking permissions on admin only commands.
        /// </summary>
        internal static string BotAdminDiscordUserId { get; private set; }
        /// <summary>
        /// The discord user id of the bot himself. Used for mentions.
        /// </summary>
        internal static string BotDiscordUserId { get; private set; }

        private static CommandService commands;
        private static IServiceProvider services;

        /// <summary>
        /// 
        /// </summary>
        public static void Initialize()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        private static void LoadConfig()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        private static void SetupDiscordBot()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="description"></param>
        /// <param name="activityType"></param>
        public static void SetClientAction(string description, ActivityType activityType)
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isScheduledAction"></param>
        /// <returns></returns>
        public static bool IsNationStatesApiActionAllowed(NationStatesApiRequestType type, bool isScheduledAction)
        {
            return false; //temporary
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNationStatesApiActionAllowed(NationStatesApiRequestType type)
        {
            return IsNationStatesApiActionAllowed(type, false);
        }
    }
}
