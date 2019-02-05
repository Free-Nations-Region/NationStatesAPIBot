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
                RequestManager.Initialize();
                if (RequestManager.Initialized)
                {
                    Logger.WriteColoredLine(LogLevel.INFO, "Initialization successfull.");
                    Console.Write("> ");
                    Run();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteColoredLine(LogLevel.ERROR, ex.ToString());
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
                    Console.Write("> ");
                }
                Thread.Sleep(25);
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Evaluate("/exit");
            Environment.Exit(0);
        }
        static Task task;
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
                    Logger.WriteColoredLine(LogLevel.INFO, "Bot is about to be stopped. Bye Bye.");
                    RequestManager.Recruiting = false;
                    //Wait to be sure that recruiting process is stopped.
                    Thread.Sleep(1500);
                    
                    running = false;
                    break;
                case "/new":
                    AddNewNationsToPending(out List<string> nations);
                    PrintNations(nations);
                    break;
                case "/dryrun":
                    Console.WriteLine($"Dry-Run Mode enabled: {RequestManager.DryRun}");
                    Console.Write($"You are about to {(RequestManager.DryRun?"dis":"en")}able the Dry Run Mode. NO API CALLS ARE MADE AS LONG DRY RUN IS ENABLED. Are you sure? (y/n)[n] ");
                    if (Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        RequestManager.DryRun = !RequestManager.DryRun;
                    }
                    Console.WriteLine();
                    Console.WriteLine($"Dry-Run Mode enabled: {RequestManager.DryRun}");
                    break;                    
                case "/recruit":
                    if (RequestManager.Recruiting)
                    {
                        Console.Write("You are about to stop the recruitment process. Are you sure? (y/n)[n] ");
                        if(Console.ReadKey().Key == ConsoleKey.Y)
                        {
                            RequestManager.Recruiting = false;
                        }
                        Console.WriteLine();
                        break;
                    }
                    else
                    {
                        RequestManager.Recruiting = true;
                        task = new Task(new Action(RequestManager.Recruit));
                        task.Start();
                        break;
                    }
                default:
                    if (line.StartsWith("/region "))
                    {
                        var region_name = line.Substring("/region ".Length);
                        nations = RequestManager.GetNationsOfRegion(region_name);
                        WriteNationsToFile(nations, $"{region_name}_initial.txt", false, false);
                        PrintNations(nations);
                        break;
                    }
                    else if (line.StartsWith("/new-in-region "))
                    {
                        var region_name = line.Substring("/new-in-region ".Length);
                        AddNewNationsFromRegionToPending(region_name, out List<string> matched);
                        PrintNations(matched);
                        break;
                    }
                    else if (line.StartsWith("/loglevel "))
                    {
                        var level_name = line.Substring("/loglevel ".Length);
                        Console.WriteLine("LogLevel: " + Enum.GetName(typeof(LogLevel), Logger.LogThreshold));
                        switch (level_name.ToUpper())
                        {
                            case "DEBUG":
                                Logger.LogThreshold = (int)LogLevel.DEBUG;
                                break;
                            case "INFO":
                                Logger.LogThreshold = (int)LogLevel.INFO;
                                break;
                            case "WARN":
                                Logger.LogThreshold = (int)LogLevel.WARN;
                                break;
                            case "ERROR":
                                Logger.LogThreshold = (int)LogLevel.ERROR;
                                break;
                            default:
                                Logger.WriteColoredLine(LogLevel.ERROR, $"Unknown LogLevel '{level_name}'");
                                break;
                        }
                        Console.WriteLine("LogLevel: " + Enum.GetName(typeof(LogLevel), Logger.LogThreshold));
                        break;
                    }
                    else
                    {
                        Logger.WriteColoredLine(LogLevel.ERROR, $"Unknown command '{line}'. Try help or ? for command reference.");
                        break;
                    }

            }
        }

        static void WriteNationsToFile(List<string> nations, string fileName, bool overwrite, bool append)
        {
            if (!append || (File.Exists(fileName) && overwrite))
            {
                File.WriteAllLines(fileName, nations);
            }
            else
            {
                File.AppendAllLines(fileName, nations);
            }
        }

        public static void AddNewNationsToPending(out List<string> nations)
        {
            nations = RequestManager.GetNewNations();
            var matched = MatchNations(nations, "pending.txt");
            Logger.WriteColoredLine(LogLevel.INFO, $"Adding {matched.Count} Nations to pending.");
            WriteNationsToFile(matched, "pending.txt", false, true);
        }

        public static void AddNewNationsFromRegionToPending(string region_name, out List<string> matched)
        {
            var nations = RequestManager.GetNationsOfRegion(region_name);
            WriteNationsToFile(nations, $"{region_name}_initial.txt", false, false);
            matched = MatchNations(nations, region_name + "_initial.txt");
            Logger.WriteColoredLine(LogLevel.INFO, $"Adding {matched.Count} Nations to pending.");
            WriteNationsToFile(matched, "pending.txt", false, true);
        }

        static List<string> MatchNations(List<string> nations, string fileName)
        {
            if (File.Exists(fileName))
            {
                var preNations = File.ReadAllLines($"{fileName}").ToList();
                preNations.Remove("");
                return nations.Except(preNations).ToList();
            }
            else
            {
                return nations;
            }
        }

        static void PrintNations(List<string> nations)
        {
            Logger.WriteColoredLine(LogLevel.INFO, "Done.");
            Console.WriteLine($"{nations.Count} nations fetched.");
            Console.Write("Do want to write them to console now? (y/n)[n]: ");
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                Console.WriteLine();
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
            Console.WriteLine("/recruit - Start recruiting process. Enter again to stop recruiting. Dry Run per default. Disable Dry Run to go productive.");
            Console.WriteLine("/dryrun - Switches Dry Run Mode. No API Calls are performed as long Dry Run Mode is enabled.");
            Console.WriteLine("/loglevel <Loglevel> - Changes Loglevel to either DEBUG, INFO, WARN or ERROR.");
        }
    }
}
