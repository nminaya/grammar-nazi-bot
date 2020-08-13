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
                throw new InvalidOperationException("Empty TELEGRAM_API_KEY");

            _client = new TelegramBotClient(apiKey);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bot Hosted Service started");

            // workaround to maintain app running in server
            while (!stoppingToken.IsCancellationRequested)
            {
                _client.StartReceiving();
                _client.OnMessage += BotOnMessage;

                await Task.Delay(60_000, stoppingToken);
                
                _client.OnMessage -= BotOnMessage;
                _client.StopReceiving();
            }
        }

        private void BotOnMessage(object sender, MessageEventArgs messageEvent)
        {
            _logger.LogError($"Message reveived: {messageEvent.Message.Text}");

            // Fire and forget for now (It returns a Task, i.e it's awaitable)
            _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Message received");
        }
    }
}