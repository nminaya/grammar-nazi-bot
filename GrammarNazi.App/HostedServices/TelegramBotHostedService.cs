using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GrammarNazi.App.HostedServices
{
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ILogger<TelegramBotHostedService> _logger;
        private readonly ITelegramBotClient _client;
        private readonly IGithubService _githubService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TelegramBotHostedService(ILogger<TelegramBotHostedService> logger,
            ITelegramBotClient telegramBotClient,
            IServiceScopeFactory serviceScopeFactory,
            IGithubService githubService)
        {
            _logger = logger;
            _client = telegramBotClient;
            _githubService = githubService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telegram Bot Hosted Service started");

            _client.StartReceiving(cancellationToken: stoppingToken);
            _client.OnMessage += async (obj, eventArgs) =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();

                    await OnMessageReceived(obj, eventArgs, scope.ServiceProvider);
                }
                catch (ApiRequestException ex)
                {
                    var warningMessages = new[] { "bot was blocked by the user", "bot was kicked from the supergroup", "have no rights to send a message" };

                    if (warningMessages.Any(x => ex.Message.Contains(x)))
                    {
                        _logger.LogWarning(ex.Message);
                    }
                    else
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);

                    // fire and forget
                    _ = _githubService.CreateBugIssue($"Application Exception: {ex.Message}", ex, GithubIssueLabels.Telegram);
                }
            };

            _client.OnCallbackQuery += async (obj, eventArgs) =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var telegramCommandHandlerService = scope.ServiceProvider.GetService<ITelegramCommandHandlerService>();

                    await telegramCommandHandlerService.HandleCallBackQuery(eventArgs.CallbackQuery);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);

                    // fire and forget
                    _ = _githubService.CreateBugIssue($"Application Exception: {ex.Message}", ex, GithubIssueLabels.Telegram);
                }
            };

            // Keep hosted service alive while receiving messages
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task OnMessageReceived(object sender, MessageEventArgs messageEvent, IServiceProvider serviceProvider)
        {
            var message = messageEvent.Message;

            if (message.Type != MessageType.Text) // We only analyze Text messages
                return;

            _logger.LogInformation($"Message received from chat id: {message.Chat.Id}");

            var chatConfig = await GetChatConfiguration(message.Chat.Id, serviceProvider);

            if (message.Text.StartsWith('/')) // Text is a command
            {
                var telegramCommandHandlerService = serviceProvider.GetService<ITelegramCommandHandlerService>();

                await telegramCommandHandlerService.HandleCommand(message);
                return;
            }

            if (chatConfig.IsBotStopped)
                return;

            var grammarService = GetConfiguredGrammarService(chatConfig, serviceProvider);

            // Remove emojis, hashtags, mentions and mentions of users without username
            var text = GetCleanedText(message);

            var corretionResult = await grammarService.GetCorrections(text);

            if (!corretionResult.HasCorrections)
                return;

            // Send "Typing..." notification
            await _client.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            var messageBuilder = new StringBuilder();

            foreach (var correction in corretionResult.Corrections)
            {
                var correctionDetailMessage = !chatConfig.HideCorrectionDetails && !string.IsNullOrEmpty(correction.Message)
                    ? $"[{correction.Message}]"
                    : string.Empty;

                messageBuilder.AppendLine($"*{correction.PossibleReplacements.First()} {correctionDetailMessage}");
            }

            await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString(), replyToMessageId: message.MessageId);
        }

        private async Task<ChatConfiguration> GetChatConfiguration(long chatId, IServiceProvider serviceProvider)
        {
            var chatConfigurationService = serviceProvider.GetService<IChatConfigurationService>();

            var chatConfig = await chatConfigurationService.GetConfigurationByChatId(chatId);

            if (chatConfig != null)
                return chatConfig;
            var messageBuilder = new StringBuilder();

            messageBuilder.AppendLine("Hi, I'm GrammarNazi.");
            messageBuilder.AppendLine("I'm currently working and correcting all spelling errors in this chat.");
            messageBuilder.AppendLine($"Type {TelegramBotCommands.Help} to get useful commands.");

            var chatConfiguration = new ChatConfiguration
            {
                ChatId = chatId,
                GrammarAlgorithm = Defaults.DefaultAlgorithm,
                SelectedLanguage = SupportedLanguages.Auto
            };

            await chatConfigurationService.AddConfiguration(chatConfiguration);
            await _client.SendTextMessageAsync(chatId, messageBuilder.ToString());

            return chatConfiguration;
        }

        private IGrammarService GetConfiguredGrammarService(ChatConfiguration chatConfig, IServiceProvider serviceProvider)
        {
            var grammarServices = serviceProvider.GetService<IEnumerable<IGrammarService>>();

            var grammarService = grammarServices.First(v => v.GrammarAlgorith == chatConfig.GrammarAlgorithm);
            grammarService.SetSelectedLanguage(chatConfig.SelectedLanguage);
            grammarService.SetStrictnessLevel(chatConfig.CorrectionStrictnessLevel);
            grammarService.SetWhiteListWords(chatConfig.WhiteListWords);

            return grammarService;
        }

        private static string GetCleanedText(Message message)
        {
            var text = GetTextWithoutMentionsWithoutUsername();

            return StringUtils.RemoveEmojis(StringUtils.RemoveHashtags(StringUtils.RemoveMentions(text)));

            string GetTextWithoutMentionsWithoutUsername()
            {
                if (message.Entities == null)
                    return message.Text;

                var messageText = message.Text;

                foreach (var entity in message.Entities.Where(v => v.Type == MessageEntityType.TextMention))
                {
                    var mention = message.Text.Substring(entity.Offset, entity.Length);

                    // Remove mention from messageText
                    messageText = messageText.Replace(mention, "");
                }

                return messageText;
            }
        }
    }
}