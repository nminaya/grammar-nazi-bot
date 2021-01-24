using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
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
        private readonly IEnumerable<IGrammarService> _grammarServices;
        private readonly IChatConfigurationService _chatConfigurationService;
        private readonly ITelegramBotClient _client;
        private readonly ITelegramCommandHandlerService _telegramCommandHandlerService;
        private readonly IGithubService _githubService;

        public TelegramBotHostedService(ILogger<TelegramBotHostedService> logger,
            ITelegramBotClient telegramBotClient,
            IEnumerable<IGrammarService> grammarServices,
            IChatConfigurationService chatConfigurationService,
            ITelegramCommandHandlerService telegramCommandHandlerService,
            IGithubService githubService)
        {
            _logger = logger;
            _grammarServices = grammarServices;
            _chatConfigurationService = chatConfigurationService;
            _client = telegramBotClient;
            _telegramCommandHandlerService = telegramCommandHandlerService;
            _githubService = githubService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telegram Bot Hosted Service started");

            _client.StartReceiving(cancellationToken: stoppingToken);
            _client.OnMessage += async (obj, eventArgs) =>
            {
                try
                {
                    await OnMessageReceived(obj, eventArgs);
                }
                catch (ApiRequestException ex)
                {
                    if (ex.Message.Contains("bot was blocked by the user"))
                    {
                        _logger.LogWarning(ex, "User has blocked the Bot");
                    }
                    else if (ex.Message.Contains("bot was kicked from the supergroup"))
                    {
                        _logger.LogWarning(ex, "Bot was kicked from supergroup");
                    }
                    else if (ex.Message.Contains("have no rights to send a message"))
                    {
                        _logger.LogWarning(ex, "Bot has no rights to send a message");
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
                    _ = _githubService.CreateBugIssue($"Application Exception: {ex.Message}", ex);
                }
            };

            _client.OnCallbackQuery += async (obj, eventArgs) =>
            {
                try
                {
                    await _telegramCommandHandlerService.HandleCallBackQuery(eventArgs.CallbackQuery);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);

                    // fire and forget
                    _ = _githubService.CreateBugIssue($"Application Exception: {ex.Message}", ex);
                }
            };

            // Keep hosted service alive while receiving messages
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task OnMessageReceived(object sender, MessageEventArgs messageEvent)
        {
            var message = messageEvent.Message;

            if (message.Type != MessageType.Text) // We only analyze Text messages
                return;

            _logger.LogInformation($"Message received from chat id: {message.Chat.Id}");

            var chatConfig = await GetChatConfiguration(message.Chat.Id);

            if (message.Text.StartsWith('/')) // Text is a command
            {
                await _telegramCommandHandlerService.HandleCommand(message);
                return;
            }

            if (chatConfig.IsBotStopped)
                return;

            var grammarService = GetConfiguredGrammarService(chatConfig);

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

        private async Task<ChatConfiguration> GetChatConfiguration(long chatId)
        {
            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(chatId);

            if (chatConfig != null)
                return chatConfig;

            var chatConfiguration = new ChatConfiguration
            {
                ChatId = chatId,
                GrammarAlgorithm = Defaults.DefaultAlgorithm,
                SelectedLanguage = SupportedLanguages.Auto
            };

            await _chatConfigurationService.AddConfiguration(chatConfiguration);

            return chatConfiguration;
        }

        private IGrammarService GetConfiguredGrammarService(ChatConfiguration chatConfig)
        {
            var grammarService = _grammarServices.First(v => v.GrammarAlgorith == chatConfig.GrammarAlgorithm);
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