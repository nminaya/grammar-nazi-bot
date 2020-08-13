using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GrammarNazi.App.HostedServices
{
    public class BotHostedService : BackgroundService
    {
        private readonly ILogger<BotHostedService> _logger;

        public BotHostedService(ILogger<BotHostedService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bot Hosted Service started");
            await Task.Delay(int.MaxValue, stoppingToken);
        }
    }
}