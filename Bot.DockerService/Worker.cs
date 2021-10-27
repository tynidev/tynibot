using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Bot;
using TyniBot;

namespace DockerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string token = Environment.GetEnvironmentVariable("discordToken");

            BotSettings settings = new BotSettings
            {
                BotToken = token
            };

            TynibotHost botHost = new TynibotHost();
            await botHost.RunAsync(settings, Log, stoppingToken);
        }

        private Task Log(Discord.LogMessage msg)
        {
            _logger.LogInformation(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
