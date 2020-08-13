using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace GrammarNazi.App.HostedServices
{
    public class BotHostedService : BackgroundService
    {
        private readonly ILogger<BotHostedService> _logger;

        // This is for test only. This will be moved to a library
        private readonly TelegramBotClient _client;

        public BotHostedService(ILogger<BotHostedService> logger)
        {
            _logger = logger;

            var apiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Empty TELEGRAM_API_KEY");

            _client = new TelegramBotClient(Environment.GetEnvironmentVariable("TELEGRAM_API_KEY"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.StartReceiving();
            _client.OnMessage += Client_OnMessage;

            _logger.LogWarning("Bot Hosted Service started");
            await Task.Delay(int.MaxValue, stoppingToken);
        }

        private void Client_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            _logger.LogError($"Message reveived: {e.Message.Text}");
            _client.SendTextMessageAsync(e.Message.Chat.Id, "Message received");
        }
    }
}