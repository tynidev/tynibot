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
            string connectionString = Environment.GetEnvironmentVariable("storageConnectionString");

            BotSettings settings = new BotSettings
            {
                BotToken = token,
                StorageConnectionString = connectionString
            };

            TynibotHost botHost = new TynibotHost();
            await botHost.RunAsync(settings, Log, stoppingToken);
        }

        private Task Log(string msg)
        {
            _logger.LogInformation(msg);
            return Task.CompletedTask;
        }
    }
}
