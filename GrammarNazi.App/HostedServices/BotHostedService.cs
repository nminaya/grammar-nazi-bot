using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

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

            _client.StartReceiving(cancellationToken: stoppingToken);
            _client.OnMessage += BotOnMessage;

            // Keep hosted service alive while receiving messages
            await Task.Delay(int.MaxValue, stoppingToken);
        }

        private void BotOnMessage(object sender, MessageEventArgs messageEvent)
        {
            _logger.LogInformation($"Message reveived: {messageEvent.Message.Text}");

            if (messageEvent.Message.Type == MessageType.Text)
            {
                // TODO: Process message text and get the spelling errors with its corrections

                // Fire and forget for now (It returns a Task, i.e it's awaitable)
                _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Message received");
            }
        }
    }
}