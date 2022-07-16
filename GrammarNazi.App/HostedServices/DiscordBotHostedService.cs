using Discord;
using Discord.Net;
using Discord.WebSocket;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GrammarNazi.App.HostedServices;

public class DiscordBotHostedService : BackgroundService
{
    private readonly BaseSocketClient _client;
    private readonly DiscordSettings _discordSettings;
    private readonly ILogger<DiscordBotHostedService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ICatchExceptionService _catchExceptionService;

    public DiscordBotHostedService(BaseSocketClient baseSocketClient,
        IOptions<DiscordSettings> options,
        ILogger<DiscordBotHostedService> logger,
        IServiceScopeFactory serviceScopeFactory,
        ICatchExceptionService catchExceptionService)
    {
        _client = baseSocketClient;
        _discordSettings = options.Value;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _catchExceptionService = catchExceptionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Discord Bot Hosted Service started");

        await _client.LoginAsync(TokenType.Bot, _discordSettings.Token);

        await _client.StartAsync();

        _client.MessageReceived += (eventArgs) =>
        {
            // fire and forget
            _ = Task.Run(async () =>
            {
                try
                {
                    await PollyExceptionHandlerHelper.HandleExceptionAndRetry<SqlException>(OnMessageReceived(eventArgs), _logger, stoppingToken);
                }
                catch (Exception ex)
                {
                    _catchExceptionService.HandleException(ex, GithubIssueLabels.Discord);
                }
            });

            return Task.CompletedTask;
        };

        // Keep hosted service alive while receiving messages
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnMessageReceived(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage message || message.Author.IsBot || message.Author.IsWebhook)
        {
            return;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        _logger.LogInformation($"Message received from channel id: {message.Channel.Id}");

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

        var corretionResult = await grammarService.GetCorrections(text);

        if (!corretionResult.HasCorrections)
        {
            return;
        }

        await message.Channel.TriggerTypingAsync();

        var messageBuilder = new StringBuilder();

        foreach (var correction in corretionResult.Corrections)
        {
            var correctionDetailMessage = !channelConfig.HideCorrectionDetails && !string.IsNullOrEmpty(correction.Message)
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
                var result = await message.Channel.SendMessageAsync(reply, messageReference: new MessageReference(replyMessageId));
                replyMessageId = result.Id;
            }

            return;
        }

        await message.Channel.SendMessageAsync(replyMessage, messageReference: new MessageReference(message.Id));
    }

    private IGrammarService GetConfiguredGrammarService(DiscordChannelConfig channelConfig, IServiceProvider serviceProvider)
    {
        var grammarServices = serviceProvider.GetService<IEnumerable<IGrammarService>>();

        var grammarService = grammarServices.First(v => v.GrammarAlgorith == channelConfig.GrammarAlgorithm);
        grammarService.SetSelectedLanguage(channelConfig.SelectedLanguage);
        grammarService.SetStrictnessLevel(channelConfig.CorrectionStrictnessLevel);
        grammarService.SetWhiteListWords(channelConfig.WhiteListWords);

        return grammarService;
    }

    private static string GetCleannedText(string text)
    {
        return StringUtils.MarkDownToPlainText(StringUtils.RemoveCodeBlocks(text));
    }

    private async Task<DiscordChannelConfig> GetChatConfiguration(SocketUserMessage message, IServiceProvider serviceProvider)
    {
        var channelConfigService = serviceProvider.GetService<IDiscordChannelConfigService>();

        var channelConfig = await channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

        if (channelConfig != null)
        {
            return channelConfig;
        }

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("Hi, I'm GrammarNazi.");
        messageBuilder.AppendLine("I'm currently working and correcting all spelling errors in this channel.");
        messageBuilder.AppendLine($"Type `{DiscordBotCommands.Help}` to get useful commands.");

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

        await message.Channel.SendMessageAsync(messageBuilder.ToString());

        return channelConfiguration;
    }
}