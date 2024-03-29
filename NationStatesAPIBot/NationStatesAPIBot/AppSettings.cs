﻿using System;
using System.Globalization;

namespace NationStatesAPIBot
{
    public class AppSettings
    {
        public const string VERSION = "v3.4.3";
        public const int API_VERSION = 11;
        public const long API_REQUEST_INTERVAL = 6000000; //0,6 s
        public const long SEND_NON_RECRUITMENTTELEGRAM_INTERVAL = 300000000; //30 s
        public const long SEND_RECRUITMENTTELEGRAM_INTERVAL = 1800000000; //3 m
        public const long REQUEST_NEW_NATIONS_INTERVAL = 18000000000; //30 m
        public const long REQUEST_REGION_NATIONS_INTERVAL = 432000000000; //12 h
        public const string BOT_ADMIN_TERM = "BotFather"; //Change to something else if you don't like the term
        public static readonly string _sleepText = $"Psst...I'm sleeping.{Environment.NewLine}{Environment.NewLine}Maintenance going on right now. Please be patient. Thank you :)";
        public static readonly string _permissionDeniedResponse = $"Sorry, but i can't do that for you. Reason: Permission denied. Contact {BOT_ADMIN_TERM} if you think that is an issue.";

        public string ClientKey { get; set; }
        public string TelegramId { get; set; }
        public string TelegramSecretKey { get; set; }
        public string Contact { get; set; }
        public string DbConnection { get; set; }
        public string DiscordBotLoginToken { get; set; }
        public ulong DiscordBotAdminUser { get; set; }
        public string NationStatesRegionName { get; set; }
        public char SeperatorChar { get; set; }
        public bool CriteriaCheckOnNations { get; set; }
        public bool EnableRecruitment { get; set; }
        public int MinimumRecruitmentPoolSize { get; set; }
        public string RegionsToRecruitFrom { get; set; }
        public string LocaleString { get; set; }

        public CultureInfo Locale
        {
            get
            {
                return !string.IsNullOrWhiteSpace(LocaleString) ? new CultureInfo(LocaleString) : CultureInfo.InvariantCulture;
            }
        }

        public string Configuration
        {
            get
            {
#if RELEASE
                return "production";
#elif DEBUG
                return "development";
#endif
            }
        }
    }
}