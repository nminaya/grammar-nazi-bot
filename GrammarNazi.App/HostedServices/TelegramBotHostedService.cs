using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.Enums;

namespace GrammarNazi.App.HostedServices
{
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ILogger<TelegramBotHostedService> _logger;
        private readonly ITelegramBotClient _client;
        private readonly IUpdateHandler _updateHandler;

        public TelegramBotHostedService(ILogger<TelegramBotHostedService> logger,
            ITelegramBotClient telegramBotClient,
            IUpdateHandler updateHandler)
        {
            _logger = logger;
            _client = telegramBotClient;
            _updateHandler = updateHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telegram Bot Hosted Service started");

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            _client.StartReceiving(
                _updateHandler.HandleUpdateAsync,
                _updateHandler.HandleErrorAsync,
                receiverOptions,
                stoppingToken);

            // Keep hosted service alive while receiving messages
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}