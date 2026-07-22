using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GrammarNazi.App.HostedServices;

public class TelegramBotHostedService : BackgroundService
{
    private readonly ILogger<TelegramBotHostedService> _logger;
    private readonly ITelegramBotClient _client;
    private readonly IUpdateHandler _updateHandler;
    private readonly Channel<Update> _updateChannel;

    private const int MaxWorkers = 5;

    public TelegramBotHostedService(ILogger<TelegramBotHostedService> logger,
        ITelegramBotClient telegramBotClient,
        IUpdateHandler updateHandler)
    {
        _logger = logger;
        _client = telegramBotClient;
        _updateHandler = updateHandler;
        _updateChannel = Channel.CreateUnbounded<Update>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Bot Hosted Service started");

        _client.StartReceiving(
            updateHandler: (botClient, update, cancellationToken) =>
            {
                _updateChannel.Writer.TryWrite(update);
                return Task.CompletedTask;
            },
            errorHandler: _updateHandler.HandleErrorAsync,
            receiverOptions: new() { AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery } },
            cancellationToken: stoppingToken);

        var workers = Enumerable.Range(0, MaxWorkers)
            .Select(_ => Task.Run(() => Worker(stoppingToken), stoppingToken));

        // Keep hosted service alive while receiving messages
        await Task.WhenAll(workers);
    }

    private async Task Worker(CancellationToken stoppingToken)
    {
        while (await _updateChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (_updateChannel.Reader.TryRead(out var update))
            {
                try
                {
                    await _updateHandler.HandleUpdateAsync(_client, update, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing update inside Telegram worker pool.");
                }
            }
        }
    }
}