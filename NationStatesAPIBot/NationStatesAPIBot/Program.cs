using Discord;
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
        public const string versionString = "v1.0";
        static bool running = true;
        static void Main(string[] args)
        {
            try
            {
                Console.Title = $"NationStatesAPIBot {versionString}";
                ActionManager.Initialize();
                if (ActionManager.Initialized)
                {
                    ActionManager.LoggerInstance.LogAsync(LogSeverity.Info, "Main", "Initialization successfull").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                ActionManager.LoggerInstance.LogAsync(LogSeverity.Critical, "Main", ex.ToString()).GetAwaiter().GetResult();
            }
        }

        static async Task RunAsync()
        {
            //Add Logic here
            ActionManager.Initialize();
            await Task.Delay(-1);
        }
    }
}
