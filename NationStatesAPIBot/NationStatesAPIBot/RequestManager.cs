using System.IO;
using System.Linq;

namespace NationStatesAPIBot
{
    public static class RequestManager
    {
        static readonly int apiVersion = 9;
        public static bool Initialized { get; private set; }
        static string clientKey;
        static string telegramID;
        static string secretKey;
        static string contact;
        static readonly string UserAgent;
        public static void Initialize()
        {
            Logger.LogThreshold = (int)LogLevel.INFO;
            Initialized = LoadConfig();
        }

        private static bool LoadConfig()
        {
            Logger.Log(LogLevel.INFO, "Trying to load config file");
            string path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}keys.config";
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
                        if (int.TryParse(lines.Find(c => c.StartsWith("contact=")).Split("=")[1], out int value))
                        {
                            Logger.LogThreshold = value;
                        }
                    }
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
                return false;
            }
        }
    }
}
