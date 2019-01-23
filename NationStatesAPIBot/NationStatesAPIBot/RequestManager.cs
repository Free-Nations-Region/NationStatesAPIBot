using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml;

namespace NationStatesAPIBot
{
    public static class RequestManager
    {
        static readonly int apiVersion = 9;

        public static DateTime lastAPIRequest;
        public static DateTime lastTelegramSending;

        public const int apiDelay = 6000000;
        public const int nonRecruitmentTelegramDelay = 300000000;
        public const int recruitmentTelegramDelay = 1800000000;

        public static bool Initialized { get; private set; }
        static string clientKey;
        static string telegramID;
        static string secretKey;
        static string contact;
        static string UserAgent;
        public static void Initialize()
        {
            Logger.LogThreshold = (int)LogLevel.INFO;
            Initialized = LoadConfig();
        }

        private static bool LoadConfig()
        {
            Logger.Log(LogLevel.INFO, "Trying to load config file");
            string path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}keys.config";
            lastAPIRequest = new DateTime(0);
            lastTelegramSending = new DateTime(0);
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path).ToList();
                if (lines.Exists(cl => cl.StartsWith("clientKey=")) &&
                   lines.Exists(t => t.StartsWith("telegramID=")) &&
                   lines.Exists(s => s.StartsWith("secretKey=")) &&
                   lines.Exists(c => c.StartsWith("contact=")))
                {
                    clientKey = lines.Find(l => l.StartsWith("clientKey=")).Split("=")[1];
                    telegramID = lines.Find(l => l.StartsWith("telegramID=")).Split("=")[1];
                    secretKey = lines.Find(l => l.StartsWith("secretKey=")).Split("=")[1];
                    contact = lines.Find(c => c.StartsWith("contact=")).Split("=")[1];
                    if (lines.Exists(c => c.StartsWith("logLevel=")))
                    {
                        if (int.TryParse(lines.Find(c => c.StartsWith("logLevel=")).Split("=")[1], out int value))
                        {
                            Logger.LogThreshold = value;
                        }
                    }
                    UserAgent = $"NationStatesAPIBot (https://github.com/drehtisch/NationStatesAPIBot) {Program.versionString} contact: {contact}";
                    return true;
                }
                else
                {
                    Logger.Log(LogLevel.ERROR, "Not all required values where specified. Please refer to documentation for information about how to configure properly.");
                    return false;
                }

            }
            else
            {
                Logger.Log(LogLevel.ERROR, $"File {path} not found.");
                Console.Write("Create file now? (y/n)[n]");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    Console.WriteLine();
                    clientKey = GetValue("Enter your clientKey: ");
                    telegramID = GetValue("Enter your telegramID: ");
                    secretKey = GetValue("Enter your secretKey: ");
                    contact = GetValue("Enter your contact: ");
                    File.WriteAllText(path,
                        $"clientKey={clientKey}{Environment.NewLine}" +
                        $"telegramID={telegramID}{Environment.NewLine}" +
                        $"secretKey={secretKey}{Environment.NewLine}" +
                        $"contact={contact}{Environment.NewLine}");
                    return true;
                }
                return false;
            }
        }

        private static string GetValue(string description)
        {
            Console.Write(description);
            var value = Console.ReadLine();
            Console.WriteLine();
            return value;
        }

        private static string ToNationID(string text)
        {
            return text.Trim().ToLower().Replace(' ', '_');
        }

        public static List<string> GetNewNations()
        {
            if (Initialized)
            {
                Logger.Log(LogLevel.INFO, "Fetching new nations. This may take a while.");
                while (DateTime.Now.Ticks - lastAPIRequest.Ticks < apiDelay) { }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.nationstates.net/cgi-bin/api.cgi?q=newnations&v=" + apiVersion);
                request.Method = "GET";
                request.UserAgent = UserAgent;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();

                lastAPIRequest = DateTime.Now;

                XmlDocument newNationsXML = new XmlDocument();
                newNationsXML.Load(responseStream);

                XmlNodeList newNationsXMLNodes = newNationsXML.GetElementsByTagName("NEWNATIONS");

                List<String> newNations = newNationsXMLNodes[0].InnerText.Split(',').ToList();
                for (int i = 0; i < newNations.Count; i++)
                {
                    newNations[i] = ToNationID(newNations[i]);
                }
                return newNations;
            }
            else
            {
                Logger.Log(LogLevel.WARN, "Ignoring GetNewNations because of RequestManager not intialized yet.");
                return new List<string>();
            }
        }

        public static List<string> GetNationsOfRegion(string region)
        {
            if (Initialized)
            {
                Logger.Log(LogLevel.INFO, $"Fetching nations of region {region}. This may take a while.");
                while (DateTime.Now.Ticks - lastAPIRequest.Ticks < apiDelay) { }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"http://www.nationstates.net/cgi-bin/api.cgi?region={region}&v={apiVersion}");
                request.Method = "GET";
                request.UserAgent = UserAgent;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();

                lastAPIRequest = DateTime.Now;

                XmlDocument newNationsXML = new XmlDocument();
                newNationsXML.Load(responseStream);

                XmlNodeList newNationsXMLNodes = newNationsXML.GetElementsByTagName("NATIONS");

                List<String> newNations = newNationsXMLNodes[0].InnerText.Split(':').ToList();
                for (int i = 0; i < newNations.Count; i++)
                {
                    newNations[i] = ToNationID(newNations[i]);
                }
                return newNations;
            }
            else
            {
                Logger.Log(LogLevel.WARN, "Ignoring GetNationsOfRegion because of RequestManager not intialized yet.");
                return new List<string>();
            }
        }
    }
}
