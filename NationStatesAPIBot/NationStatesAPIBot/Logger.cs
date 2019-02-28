using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NationStatesAPIBot
{
    public class Logger
    {
        public LogSeverity SeverityThreshold { get; set; } = LogSeverity.Info;
        public async Task LogAsync(LogSeverity logSeverity, string source, string text)
        {
            if (logSeverity <= SeverityThreshold)
            {
                ConsoleColor color = ConsoleColor.Gray;
                if (logSeverity == LogSeverity.Critical)
                {
                    color = ConsoleColor.Red;
                }
                else if (logSeverity == LogSeverity.Error)
                {
                    color = ConsoleColor.DarkRed;
                }
                else if (logSeverity == LogSeverity.Warning)
                {
                    color = ConsoleColor.Yellow;
                }
                else if (logSeverity == LogSeverity.Info)
                {
                    color = ConsoleColor.Magenta;
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
                try
                {
                    await File.AppendAllLinesAsync($"log_{DateTime.Now.Year}{DateTime.Now.Month}{DateTime.Now.Day}.txt", new List<string>() { message });
                }
                catch(Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[{DateTime.Now} at {source}] {LogSeverity.Error} : {ex.ToString()}");
                }
            }
        }
    }
}
