using Microsoft.Extensions.Logging;
using NationStatesAPIBot.Interfaces;
using System;
using System.Threading.Tasks;

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

        public async Task Run()
        {
            try
            {
                _logger.LogInformation($"--- App started ---");
                await _botService.RunAsync();
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
