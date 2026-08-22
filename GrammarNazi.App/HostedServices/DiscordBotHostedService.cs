using Discord;
using Discord.WebSocket;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GrammarNazi.App.HostedServices;

public partial class DiscordBotHostedService(BaseSocketClient baseSocketClient,
    IOptions<DiscordSettings> options,
    ILogger<DiscordBotHostedService> logger,
    IServiceScopeFactory serviceScopeFactory,
    ICatchExceptionService catchExceptionService) : BackgroundService
{
    private readonly DiscordSettings _discordSettings = options.Value;
    private readonly Channel<SocketMessage> _messageChannel = Channel.CreateUnbounded<SocketMessage>();

    private const int MaxWorkers = 5;

    [LoggerMessage(Level = LogLevel.Information, Message = "Message received from channel id: {ChannelId}")]
    private static partial void LogMessageReceived(ILogger logger, ulong channelId);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Discord Bot Hosted Service started");

        await baseSocketClient.LoginAsync(TokenType.Bot, _discordSettings.Token);

        await baseSocketClient.StartAsync();

        Task OnMessageReceivedEvent(SocketMessage eventArgs)
        {
            _messageChannel.Writer.TryWrite(eventArgs);
            return Task.CompletedTask;
        }

        baseSocketClient.MessageReceived += OnMessageReceivedEvent;

        try
        {
            var workers = Enumerable.Range(0, MaxWorkers)
                .Select(_ => Task.Run(() => Worker(stoppingToken), stoppingToken));

            // Keep hosted service alive while receiving messages
            await Task.WhenAll(workers);
        }
        finally
        {
            baseSocketClient.MessageReceived -= OnMessageReceivedEvent;
        }
    }

    private async Task Worker(CancellationToken stoppingToken)
    {
        while (await _messageChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (_messageChannel.Reader.TryRead(out var message))
            {
                try
                {
                    await OnMessageReceived(message);
                }
                catch (Exception ex)
                {
                    catchExceptionService.HandleException(ex, GithubIssueLabels.Discord);
                }
            }
        }
    }

    private async Task OnMessageReceived(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage message
            || message.Author.IsBot
            || message.Author.IsWebhook
            || string.IsNullOrWhiteSpace(message.Content))
        {
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        LogMessageReceived(logger, message.Channel.Id);

        var channelConfig = await GetChatConfiguration(message, serviceProvider);

        // Text is a command
        if (message.Content.StartsWith(DiscordBotCommands.Prefix))
        {
            var commandHandler = serviceProvider.GetService<IDiscordCommandHandlerService>();
            await commandHandler.HandleCommand(message);
            return;
        }

        if (channelConfig.IsBotStopped)
        {
            return;
        }

        var grammarService = GetConfiguredGrammarService(channelConfig, serviceProvider);

        var text = GetCleannedText(message.Content);

        var correctionResult = await grammarService.GetCorrections(text);

        if (!correctionResult.HasCorrections)
        {
            return;
        }

        await message.Channel.TriggerTypingAsync();

        var messageBuilder = new StringBuilder();

        foreach (var correction in correctionResult.Corrections)
        {
            var correctionDetailMessage =
                !channelConfig.HideCorrectionDetails && !string.IsNullOrEmpty(correction.Message)
                    ? $"[{correction.Message}]"
                    : string.Empty;

            messageBuilder.AppendLine($"*{correction.PossibleReplacements.First()} {correctionDetailMessage}");
        }

        var replyMessage = messageBuilder.ToString();

        if (replyMessage.Length >= Defaults.DiscordTextMaxLength) // Split the reply in various messages
        {
            var replyMessages = replyMessage.SplitInParts(Defaults.DiscordTextMaxLength);

            var replyMessageId = message.Id;

            foreach (var reply in replyMessages)
            {
                var result =
                    await message.Channel.SendMessageAsync(reply,
                        messageReference: new MessageReference(replyMessageId));
                replyMessageId = result.Id;
            }

            return;
        }

        await message.Channel.SendMessageAsync(replyMessage, messageReference: new MessageReference(message.Id));
    }

    private static IGrammarService GetConfiguredGrammarService(DiscordChannelConfig channelConfig,
        IServiceProvider serviceProvider)
    {
        var grammarServices = serviceProvider.GetService<IEnumerable<IGrammarService>>();

        var grammarService = grammarServices.First(v => v.GrammarAlgorithm == channelConfig.GrammarAlgorithm);
        grammarService.SetSelectedLanguage(channelConfig.SelectedLanguage);
        grammarService.SetStrictnessLevel(channelConfig.CorrectionStrictnessLevel);
        grammarService.SetWhiteListWords(channelConfig.WhiteListWords);

        return grammarService;
    }

    private static string GetCleannedText(string text)
    {
        return StringUtils.MarkDownToPlainText(StringUtils.RemoveCodeBlocks(text));
    }

    private static async Task<DiscordChannelConfig> GetChatConfiguration(SocketUserMessage message,
        IServiceProvider serviceProvider)
    {
        var channelConfigService = serviceProvider.GetService<IDiscordChannelConfigService>();

        var channelConfig = await channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

        if (channelConfig != null)
        {
            return channelConfig;
        }

        var welcomeMessage = $"""
            Hi, I'm GrammarNazi.
            I'm currently working and correcting all spelling errors in this channel.
            Type `{DiscordBotCommands.Help}` to get useful commands.

            """;

        ulong guild = message.Channel switch
        {
            SocketDMChannel dmChannel => dmChannel.Id,
            SocketGuildChannel guildChannel => guildChannel.Guild.Id,
            _ => default
        };

        var channelConfiguration = new DiscordChannelConfig
        {
            ChannelId = message.Channel.Id,
            GrammarAlgorithm = Defaults.DefaultAlgorithm,
            Guild = guild,
            SelectedLanguage = SupportedLanguages.Auto
        };

        await channelConfigService.AddConfiguration(channelConfiguration);

        await message.Channel.SendMessageAsync(welcomeMessage);

        return channelConfiguration;
    }
}
