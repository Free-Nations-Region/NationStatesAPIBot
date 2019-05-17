using System;
using System.Collections.Generic;
using System.Text;

namespace NationStatesAPIBot
{
    public class AppSettings
    {
        public string ClientKey { get; set; }
        public string TelegramId { get; set; }
        public string TelegramSecretKey { get; set; }
        public string Contact { get; set; }
        public string DbConnection { get; set; }
        public string DiscordBotLoginToken { get; set; }
        public string DiscordBotAdminUser { get; set; }
        public string NationStatesRegionName { get; set; }
        public bool EnableCitizenshipOverTime { get; set; }
        public int RequiredDaysForCitizenship { get; set; }
    }
}
