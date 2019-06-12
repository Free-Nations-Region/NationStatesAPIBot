using Microsoft.Extensions.Logging;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;

namespace NationStatesAPIBot
{
    public static class LogMessageBuilder
    {
        public static string Build(EventId eventId, string message)
        {
            return $"[{eventId.Id}] {message}";
        }
    }
}
