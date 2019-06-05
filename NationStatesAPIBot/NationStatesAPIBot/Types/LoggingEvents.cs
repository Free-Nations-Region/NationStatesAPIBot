using System.Text;

namespace NationStatesAPIBot.Types
{
    public enum LoggingEvents
    {
        //Everything below 10000 is reserved for random log event ids
        DiscordLogEvent = 10000,
        UserMessage = 10100
    }
}
