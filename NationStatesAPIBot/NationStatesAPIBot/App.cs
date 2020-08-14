using Microsoft.Extensions.Logging;
using NationStatesAPIBot.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NationStatesAPIBot.Services;

namespace NationStatesAPIBot
{
    public class App
    {
        private readonly IBotService _botService;
        private readonly ILogger<App> _logger;

        public App(IBotService botService, ILogger<App> logger)
        {
            _botService = botService;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            try
            {
                _logger.LogInformation($"--- App started ---");
                await _botService.RunAsync();
                Program.ServiceProvider.GetService<DumpDataService>().StartDumpUpdateCycle();
                while (_botService.IsRunning)
                {
                    await Task.Delay(10000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "A critical error occured.");
            }
            finally
            {
                _logger.LogInformation("Press any key to quit.");
                Console.ReadKey();
            }
        }
    }
}