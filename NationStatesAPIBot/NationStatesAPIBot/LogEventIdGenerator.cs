using Microsoft.Extensions.Logging;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;

namespace NationStatesAPIBot
{
    public static class LogEventIdProvider
    {
       static Random _rnd = new Random();
        static HashSet<int> UsedLogEventIds = new HashSet<int>();
        public static EventId GetRandomLogEventId()
        {
            int max = 10000;
            var id = _rnd.Next(0, max);
            while (UsedLogEventIds.Contains(id))
            {
                id = _rnd.Next(0, max);
            }
            return new EventId(id);
        }

        public static void ReleaseEventId(EventId eventId)
        {
            if (UsedLogEventIds.Contains(eventId.Id))
            {
                UsedLogEventIds.Remove(eventId.Id);
            }
        }

        public static EventId GetEventIdByType(LoggingEvent loggingEvent)
        {
            return new EventId((int)loggingEvent, loggingEvent.ToString());
        }
    }
}
