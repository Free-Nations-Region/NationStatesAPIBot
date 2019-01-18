using System;
using System.Collections.Generic;
using System.Text;

namespace NationStatesAPIBot
{
    public enum LogLevel
    {
        DEBUG = 30000,
        INFO = 40000,
        WARN = 50000,
        ERROR = 60000
    }

    public class Logger
    {
        public static int LogThreshold { get; set; }
        public static void Log(LogLevel level, string text)
        {
            if ((int)level >= LogThreshold)
            {
                ConsoleColor color = ConsoleColor.Gray;
                if (level == LogLevel.ERROR)
                {
                    color = ConsoleColor.Red;
                }
                else if (level == LogLevel.WARN)
                {
                    color = ConsoleColor.Yellow;
                }
                else if (level == LogLevel.INFO)
                {
                    color = ConsoleColor.Cyan;
                }
                else if (level == LogLevel.DEBUG)
                {
                    color = ConsoleColor.Green;
                }
                Console.ForegroundColor = color;
                Console.WriteLine($"{level.ToString()}: {text}");
                Console.ResetColor();
            }
        }
    }
}
