using Discord;
using NationStatesAPIBot.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NationStatesAPIBot
{
    class Program
    {
        public const string versionString = "v2.0";
        static void Main(string[] args)
        {
            try
            {
                Console.CancelKeyPress += Console_CancelKeyPress;
                Console.Title = $"NationStatesAPIBot {versionString}";
                Task.Run(() => RunAsync()).Wait();
            }
            catch (Exception ex)
            {
                Task.Run(() => ActionManager.LoggerInstance.LogAsync(LogSeverity.Critical, "Main", ex.ToString())).Wait();
                Task.Run(() => Console.Out.WriteLineAsync("Press any key to quit.")).Wait();
                Console.ReadKey();
            }
        }

        private static async void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            await ActionManager.Shutdown();
            
        }

        static async Task RunAsync()
        {
            await ActionManager.StartUp();
            while (ActionManager.Running)
            {
                await Task.Delay(10000);
            }
        }
    }
}
