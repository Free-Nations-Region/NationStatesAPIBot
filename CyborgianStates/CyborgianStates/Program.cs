using CyborgianStates.Interfaces;
using CyborgianStates.Repositories;
using CyborgianStates.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NetEscapades.Extensions.Logging.RollingFile;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace CyborgianStates
{
    class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static string BuildConfig { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Stil", "IDE0060:Nicht verwendete Parameter entfernen", Justification = "<Ausstehend>")]
        static void Main(string[] args)
        {
            DetermineConfiguration();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
            Console.CancelKeyPress += Console_CancelKeyPress;
            
            ServiceProvider.GetService<App>().Run().Wait();
        }

        private static void DetermineConfiguration()
        {
            BuildConfig = "production";
#if DEBUG
            BuildConfig = "development";
#endif
        }

        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{BuildConfig}.json", false, false)
                .Build();
            serviceCollection.AddOptions();
            serviceCollection.Configure<AppSettings>(configuration.GetSection("Configuration"));
            //configuration.GetSection("Configuration");
            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                loggingBuilder.SetMinimumLevel(LogLevel.Information);
                loggingBuilder.AddConsole();
                loggingBuilder.AddFile(options =>
                {
                    options.FileName = "bot-";
                    options.Extension = "log";
                    options.RetainedFileCountLimit = null;
                    options.Periodicity = PeriodicityOptions.Daily;
                });
            });

            var database = GetMongoDbDatabase(configuration.GetSection("Configuration"));

            // add services
            serviceCollection.AddSingleton(typeof(IMongoDatabase), database);
            serviceCollection.AddSingleton<IBotService, DiscordBotService>();
            serviceCollection.AddSingleton<NationStatesApiService, NationStatesApiService>();
            serviceCollection.AddSingleton<DumpDataService, DumpDataService>();
            serviceCollection.AddSingleton<IPermissionRepository, PermissionRepository>();
            serviceCollection.AddSingleton<INationRepository, NationRepository>();
            serviceCollection.AddSingleton<IUserRepository, UserRepository>();
            // add app
            serviceCollection.AddTransient<App>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Ausstehend>")]
        private static IMongoDatabase GetMongoDbDatabase(IConfigurationSection configurationSection)
        {
            try
            {
                var connectionString = configurationSection.GetValue<string>("DbConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("DbConnection not set in appsettings.");
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("DbConnection lacks of a databasename.");
                var _databaseName = MongoUrl.Create(connectionString).DatabaseName;
                IMongoClient mongoClient = new MongoClient(connectionString);
                IMongoDatabase mongoDatabase = mongoClient.GetDatabase(_databaseName);
                return mongoDatabase;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error while connecting to the database.");
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
                Environment.Exit(-1);
                return null;
            }
        }

        private static async void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.ResetColor();
            await ServiceProvider.GetService<IBotService>().ShutdownAsync();
        }
    }
}
