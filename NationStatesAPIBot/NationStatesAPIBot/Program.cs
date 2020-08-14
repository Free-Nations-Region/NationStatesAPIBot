using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NationStatesAPIBot.Interfaces;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Services;
using NetEscapades.Extensions.Logging.RollingFile;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NationStatesAPIBot
{
    internal class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static DateTime StartTime { get; private set; }

        private static async Task Main(string[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
            Console.CancelKeyPress += Console_CancelKeyPressAsync;
            StartTime = DateTime.UtcNow;
            await ServiceProvider.GetService<App>().RunAsync();
        }

        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            string configurationName = "production";
#if DEBUG
            configurationName = "development";
#endif

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{configurationName}.json", false, true)
                .Build();
            serviceCollection.AddOptions();
            serviceCollection.Configure<AppSettings>(configuration.GetSection("Configuration"));
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
            // add services
            serviceCollection.AddSingleton<IBotService, DiscordBotService>();
            serviceCollection.AddSingleton<NationStatesApiService, NationStatesApiService>();
            serviceCollection.AddSingleton<DumpDataService, DumpDataService>();
            serviceCollection.AddSingleton<IPermissionManager, PermissionManager>();
            serviceCollection.AddSingleton<RecruitmentService, RecruitmentService>();
            // add app
            serviceCollection.AddTransient<App>();
        }

        private static async void Console_CancelKeyPressAsync(object sender, ConsoleCancelEventArgs e)
        {
            Console.ResetColor();
            await ServiceProvider.GetService<IBotService>().ShutdownAsync();
        }
    }
}