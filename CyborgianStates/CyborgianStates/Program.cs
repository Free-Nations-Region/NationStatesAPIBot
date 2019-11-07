﻿using CyborgianStates.Interfaces;
using CyborgianStates.Repositories;
using CyborgianStates.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        public static DateTime StartTime { get; private set; }
        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
            Console.CancelKeyPress += Console_CancelKeyPress;
            StartTime = DateTime.UtcNow;
            await ServiceProvider.GetService<App>().Run();
        }
        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            string configurationName = "production";
#if DEBUG
            configurationName = "development";
#endif
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{configurationName}.json", false, false)
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
            serviceCollection.AddSingleton<IPermissionRepository, PermissionRepository>();
            // add app
            serviceCollection.AddTransient<App>();
        }

        private static async void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.ResetColor();
            await ServiceProvider.GetService<IBotService>().ShutdownAsync();
        }
    }
}
