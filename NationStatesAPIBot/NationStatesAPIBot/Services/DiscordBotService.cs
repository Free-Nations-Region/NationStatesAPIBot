using Microsoft.Extensions.Logging;
using NationStatesAPIBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Services
{
    public class DiscordBotService : IBotService
    {
        private readonly ILogger<DiscordBotService> _logger;
        public DiscordBotService(ILogger<DiscordBotService> logger)
        {
            _logger = logger;
        }
        public Task RunAsync()
        {
            _logger.LogInformation("DiscordBotService started successfully");
            return Task.CompletedTask;
        }
    }
}
