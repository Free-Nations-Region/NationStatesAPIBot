using Discord;
using System;
using System.Threading.Tasks;

namespace NationStatesAPIBot
{
    public class Logger
    {
        public LogSeverity SeverityThreshold { get; set; } = LogSeverity.Info;
        public Task LogAsync(LogSeverity logSeverity, string source, string text)
        {
            if (logSeverity <= SeverityThreshold)
            {
                ConsoleColor color = ConsoleColor.Gray;
                if (logSeverity == LogSeverity.Critical)
                {
                    color = ConsoleColor.DarkRed;
                }
                else if (logSeverity == LogSeverity.Error)
                {
                    color = ConsoleColor.Red;
                }
                else if (logSeverity == LogSeverity.Warning)
                {
                    color = ConsoleColor.Yellow;
                }
                else if (logSeverity == LogSeverity.Info)
                {
                    color = ConsoleColor.Blue;
                }
                else if (logSeverity == LogSeverity.Debug)
                {
                    color = ConsoleColor.Green;
                }
                else if (logSeverity == LogSeverity.Verbose)
                {
                    color = ConsoleColor.Cyan;
                }
                Console.ForegroundColor = color;
                string message = $"[{DateTime.Now} at {source}] {logSeverity} : {text}";
                Console.WriteLine(message);
                Console.ResetColor();
                return Task.CompletedTask;
            }
            else
            {
                return Task.CompletedTask;
            }
        }
    }
}
