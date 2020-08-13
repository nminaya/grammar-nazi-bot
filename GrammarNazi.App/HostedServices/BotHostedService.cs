using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace GrammarNazi.App.HostedServices
{
    public class BotHostedService : BackgroundService
    {
        private readonly ILogger<BotHostedService> _logger;

        private readonly TelegramBotClient _client;

        public BotHostedService(ILogger<BotHostedService> logger)
        {
            _logger = logger;

            var apiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Empty TELEGRAM_API_KEY");

            _client = new TelegramBotClient(apiKey);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.StartReceiving();
            _client.OnMessage += BotOnMessage;

            _logger.LogInformation("Bot Hosted Service started");
            await Task.Delay(int.MaxValue, stoppingToken);
        }

        private void BotOnMessage(object sender, MessageEventArgs messageEvent)
        {
            _logger.LogError($"Message reveived: {messageEvent.Message.Text}");

            // Fire and forget for now (It returns a Task, i.e it's awaitable)
            _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Message received");
        }
    }
}