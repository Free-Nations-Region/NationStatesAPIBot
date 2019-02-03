using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Xml;

namespace NationStatesAPIBot
{
    public static class RequestManager
    {
        static readonly int apiVersion = 9;

        static DateTime lastAPIRequest;
        static DateTime lastTelegramSending;
        static DateTime lastNewNationRequest;
        static DateTime lastRegionNationsRequest;

        public const long apiDelay = 6000000; //0,6 s
        public const long nonRecruitmentTelegramDelay = 300000000; //30 s
        public const long recruitmentTelegramDelay = 1800000000; //3 m 1800000000
        public const long newNationsRequestDelay = 18000000000; //30 m 36000000000
        public const long matchNewNationsInRegionRequestDelay = 432000000000; //12 h 432000000000
        public static bool Initialized { get; private set; }
        public static bool Recruiting { get; set; }
        static string clientKey;
        static string telegramID;
        static string secretKey;
        static string contact;
        static string UserAgent;

        static List<string> PendingNations = new List<string>();
        public static void Initialize()
        {
            Logger.LogThreshold = (int)LogLevel.INFO;
            Initialized = LoadConfig();
            LoadPendingNations();
        }

        private static void LoadPendingNations()
        {
            if (File.Exists("pending.txt"))
            {
                var lines = File.ReadAllLines("pending.txt");
                var toAdd = lines.Except(PendingNations);
                PendingNations.AddRange(toAdd);
            }
            else
            {
                Logger.Log(LogLevel.INFO, "pending file does not exist.");
            }
        }

        private static void WritePending()
        {
            File.WriteAllLines("pending.txt", PendingNations);
        }

        private static void WriteSend(string nation)
        {
            File.AppendAllText("send.txt",$"{DateTime.Now},{nation}{Environment.NewLine}" );
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

        public static List<string> GetNewNations()
        {
            if (Initialized && !DryRun)
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

                List<string> newNations = newNationsXMLNodes[0].InnerText.Split(',').ToList();
                for (int i = 0; i < newNations.Count; i++)
                {
                    newNations[i] = ToID(newNations[i]);
                }
                return newNations;
            }
            else
            {
                Logger.Log(LogLevel.INFO, "Ignoring GetNewNations because of RequestManager not intialized yet or currently in Dry-Run mode.");
                return new List<string>();
            }
        }

        public static List<string> GetNationsOfRegion(string region)
        {
            if (Initialized && !DryRun)
            {
                Logger.Log(LogLevel.INFO, $"Fetching nations of region {region}. This may take a while.");
                while (DateTime.Now.Ticks - lastAPIRequest.Ticks < apiDelay) { }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"http://www.nationstates.net/cgi-bin/api.cgi?region={ToID(region)}&v={apiVersion}");
                request.Method = "GET";
                request.UserAgent = UserAgent;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();

                lastAPIRequest = DateTime.Now;

                XmlDocument newNationsXML = new XmlDocument();
                newNationsXML.Load(responseStream);

                XmlNodeList newNationsXMLNodes = newNationsXML.GetElementsByTagName("NATIONS");

                List<string> newNations = newNationsXMLNodes[0].InnerText.Split(':').ToList();
                for (int i = 0; i < newNations.Count; i++)
                {
                    newNations[i] = ToID(newNations[i]);
                }
                return newNations;
            }
            else
            {
                Logger.Log(LogLevel.INFO, "Ignoring GetNationsOfRegion because of RequestManager not intialized yet or currently in Dry-Run mode.");
                return new List<string>();
            }
        }

        public static bool DryRun { get; set; } = true;
        public static void Recruit()
        {
            Logger.Log(LogLevel.INFO, "Starting recruitment.");
            while (Recruiting)
            {
                Logger.Log(LogLevel.DEBUG, "Recruitment Loop");
                if (DateTime.Now.Ticks - lastTelegramSending.Ticks > recruitmentTelegramDelay)
                {
                    Logger.Log(LogLevel.DEBUG, "Sending Telegram.");
                    if (!DryRun)
                    {
                        LoadPendingNations();
                    }
                    var picked = PendingNations.Take(1);
                    var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                    if (nation != null)
                    {
                        Logger.Log(LogLevel.DEBUG, $"Sending Telegram to {nation}");
                        if (SendTelegram(nation))
                        {
                            PendingNations.Remove(nation);
                            WriteSend(nation);
                            WritePending();
                        }
                        if (DryRun)
                        {
                            lastTelegramSending = DateTime.Now;
                            PendingNations.Remove(nation);
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.WARN, "Pending Nations empty can not send telegram: No recipient.");
                    }
                }
                if (DateTime.Now.Ticks - lastNewNationRequest.Ticks > newNationsRequestDelay)
                {
                    Logger.Log(LogLevel.DEBUG, "Collecting New Nations.");
                    Program.AddNewNationsToPending(out List<string> nations);
                    lastNewNationRequest = DateTime.Now;
                }
                if (DateTime.Now.Ticks - lastRegionNationsRequest.Ticks > matchNewNationsInRegionRequestDelay)
                {
                    Logger.Log(LogLevel.DEBUG, "Collecting Rejected Nations.");
                    Program.AddNewNationsFromRegionToPending("the_rejected_realms", out List<string> nations);
                    lastRegionNationsRequest = DateTime.Now;
                }
                Thread.Sleep(1000);
            }
            Logger.Log(LogLevel.INFO, "Recruiting stopped.");
        }

        private static bool SendTelegram(string recipient)
        {
            try
            {
                if (Initialized && !DryRun)
                {
                    while (DateTime.Now.Ticks - lastTelegramSending.Ticks < recruitmentTelegramDelay) { }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.nationstates.net/cgi-bin/api.cgi?a=sendTG&client=" + HttpUtility.UrlEncode(clientKey) + "&tgid=" + HttpUtility.UrlEncode(telegramID) + "&key=" + HttpUtility.UrlEncode(secretKey) + "&to=" + HttpUtility.UrlEncode(ToID(recipient)));
                    request.Method = "GET";
                    request.UserAgent = UserAgent;

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    StreamReader responseStreamReader = new StreamReader(responseStream);
                    string responseText = responseStreamReader.ReadToEnd();

                    if (!responseText.Contains("queued"))
                        throw new Exception("NationStates reported an error: " + responseText);

                    response.Close();

                    lastTelegramSending = DateTime.Now;
                    return true;
                }
                else
                {
                    Logger.Log(LogLevel.INFO, "Ignoring SendTelegram because of RequestManager not intialized yet or currently in Dry-Run mode.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                lastTelegramSending = DateTime.Now;
                Logger.Log(LogLevel.ERROR, "Failed to queue telegram for " + FromID(recipient) + "! (" + ex.GetType() + ": " + ex.Message + " - " + ex.StackTrace + ")");
                return false;
            }
        }

        private static string ToID(string text)
        {
            return text.Trim().ToLower().Replace(' ', '_');
        }

        private static string FromID(string text)
        {
            return text.Trim().ToLower().Replace('_', ' ');
        }
    }
}
