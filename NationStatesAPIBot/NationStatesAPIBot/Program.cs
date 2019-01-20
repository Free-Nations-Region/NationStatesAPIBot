using System;
using System.Collections.Generic;
using System.Threading;

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
                RequestManager.Initialize();
                if (RequestManager.Initialized)
                {
                    Logger.Log(LogLevel.INFO, "Initialization successfull.");
                    Run();
                }
                if (running)
                {
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.ERROR, ex.ToString());
            }
        }

        static void Run()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    Evaluate(Console.ReadLine());
                }
                Thread.Sleep(25);
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Evaluate("/exit");
        }

        static void Evaluate(string line)
        {
            switch (line)
            {
                case "/help":
                case "?":
                    PrintHelp();
                    break;
                case "/exit":
                case "/quit":
                    running = false;
                    break;
                case "/new":
                    var nations = RequestManager.GetNewNations();
                    PrintNations(nations);
                    break;
                default:
                    Logger.Log(LogLevel.ERROR, $"Unknown command '{line}'");
                    break;
            }
        }

        static void PrintNations(List<string> nations)
        {
            Logger.Log(LogLevel.INFO, "Done.");
            Console.WriteLine($"{nations.Count} nations fetched.");
            Console.Write("Do want to write them to console now? (y/n)[n]: ");
            Console.WriteLine();
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                foreach (string nation in nations)
                {
                    Console.WriteLine(nation);
                }
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine($"Available Commands in version {versionString}:");
            Console.WriteLine("/help, ? - Shows this help.");
            Console.WriteLine("/exit, /quit - Terminates this program.");
            Console.WriteLine("/new - Fetches all new nations and prints them out.");
            Console.WriteLine("/region <region> - Fetches all nations from specific region and prints them out.");
        }
    }
}
