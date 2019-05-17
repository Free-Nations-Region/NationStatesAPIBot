using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly AppSettings _config;
        public DiscordBotService(ILogger<DiscordBotService> logger, IOptions<AppSettings> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public Task IsRelevantAsync()
        {
            throw new NotImplementedException();
        }

        public Task IsRelevantAsync(object message, object user)
        {
            throw new NotImplementedException();
        }

        public Task ProcessMessage(object message)
        {
            throw new NotImplementedException();
        }

        public Task RunAsync()
        {
            _logger.LogInformation($"--- DiscordBotService started ---");
            return Task.CompletedTask;
        }
    }
}
