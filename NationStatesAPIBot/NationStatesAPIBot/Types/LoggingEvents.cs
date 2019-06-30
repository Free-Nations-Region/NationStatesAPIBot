namespace NationStatesAPIBot.Types
{
    public enum LoggingEvent
    {
        //Everything below 10000 is reserved for random log event ids
        DiscordLogEvent = 10000,
        UserDbAction = 10050,
        UserMessage = 10100,
        PermissionDenied = 10200,
        GetNationStats = 10300,
        GetRegionStats = 10400,
        RNCommand = 10500,
        RNSCommand = 10550,
        GetNewNations = 10600,
        GetEndorsedBy = 10700,
        GetNationsEndorsed = 10800,
        APIRecruitment = 11000,
        WouldReceiveTelegram = 11100,
        GetRecruitableNations = 11200,
        SendRecruitmentTelegram = 12000,
        DumpDataServiceAction = 13000,
    }
}
