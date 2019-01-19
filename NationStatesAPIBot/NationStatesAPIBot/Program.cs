using System;
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
            while (running)
            {
                Console.CancelKeyPress += Console_CancelKeyPress;
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
                    foreach(string nation in nations)
                    {
                        Console.WriteLine(nation);
                    }
                    Logger.Log(LogLevel.INFO, "Done.");
                    break;
                default:
                    Logger.Log(LogLevel.ERROR, $"Unknown command '{line}'");
                    break;
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine($"Available Commands in version {versionString}:");
            Console.WriteLine("/help, ? - Shows this help.");
            Console.WriteLine("/exit, /quit - Terminates this program.");
            Console.WriteLine("/new - Fetches all new nations and prints them out.");
        }
    }
}
