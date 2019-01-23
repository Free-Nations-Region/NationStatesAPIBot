using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.ERROR, ex.ToString());
               
            }
            if (running)
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
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
                    WriteNationsToFile(nations, "pending", false, true);
                    break;
                default:
                    if (line.StartsWith("/region"))
                    {
                        var region_name = line.Substring("/region ".Length);
                        nations = RequestManager.GetNationsOfRegion(region_name);
                        PrintNations(nations);
                        WriteNationsToFile(nations, $"{region_name}_initial", false, false);
                        break;
                    }
                    else if (line.StartsWith("/new-in-region"))
                    {
                        var region_name = line.Substring("/new-in-region ".Length);
                        nations = RequestManager.GetNationsOfRegion(region_name);
                        WriteNationsToFile(nations, $"{region_name}_initial", false, false);
                        var matched = MatchNations(nations, region_name);
                        PrintNations(matched);
                        WriteNationsToFile(nations, "pending", false, true);
                        break;
                    }
                    else
                    {
                        Logger.Log(LogLevel.ERROR, $"Unknown command '{line}'");
                        break;
                    }
                    
            }
        }

        static void WriteNationsToFile(List<string> nations, string fileName, bool overwrite, bool append)
        {
            if (!File.Exists(fileName) || overwrite)
            {
                if (!append)
                {
                    File.WriteAllLines(fileName, nations);
                }
                else
                {
                    File.AppendAllLines(fileName, nations);
                }
            }
        }

        static List<string> MatchNations(List<string> nations, string regionName)
        {
            List<string> result = new List<string>();
            var preNations = File.ReadAllLines($"{regionName}_initial").ToList();
            preNations.Remove("");
            foreach(string nation in nations)
            {
                if (!preNations.Contains(nation))
                {
                    result.Add(nation);
                }
            }
            return result;
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
            Console.WriteLine("/new-in-region <region> - Fetches all nations from specific region and matches them with nations of that region fetched before.");
        }
    }
}
