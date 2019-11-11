﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CyborgianStates
{
    public class AppSettings
    {
        public const string VERSION = "v4.0-Pre-2";
        public const int API_VERSION = 9;
        public const long API_REQUEST_INTERVAL = 6000000; //0,6 s
        public const long SEND_RECRUITMENTTELEGRAM_INTERVAL = 1800000000; //3 m
        public const long REQUEST_NEW_NATIONS_INTERVAL = 18000000000; //30 m 
        public const string BOT_ADMIN_TERM = "BotFather"; //Change to something else if you don't like the term
        public static readonly string SLEEPTEXT = $"Psst...I'm sleeping.{Environment.NewLine}{Environment.NewLine}Maintenance going on right now. Please be patient. Thank you :)";
        public static readonly string PERMISSION_DENIED_RESPONSE = $"Sorry, but i can't do that for you. Reason: Permission denied. Contact {BOT_ADMIN_TERM} if you think that is an issue.";

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
        public CultureInfo Locale { get; set; }
        public static string Configuration
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