using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GrammarNazi.Core.Utilities
{
    public class TelegramUpdateHandler : IUpdateHandler
    {
        private readonly ILogger<TelegramUpdateHandler> _logger;
        private readonly IGithubService _githubService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TelegramUpdateHandler(IServiceScopeFactory serviceScopeFactory, IGithubService githubService, ILogger<TelegramUpdateHandler> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _githubService = githubService;
            _logger = logger;
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is ApiRequestException apiRequestException)
            {
                var warningMessages = new[] { "bot was blocked by the user", "bot was kicked from the supergroup", "have no rights to send a message" };

                if (warningMessages.Any(x => apiRequestException.Message.Contains(x)))
                {
                    _logger.LogWarning(apiRequestException.Message);
                }
                else
                {
                    _logger.LogError(apiRequestException, apiRequestException.Message);
                }

                return Task.CompletedTask;
            }
            else if (exception is HttpRequestException requestException
                && requestException.StatusCode == HttpStatusCode.BadGateway)
            {
                _logger.LogWarning("Bad Gateway", requestException);

                return Task.CompletedTask;
            }

            var innerExceptions = exception.GetInnerExceptions();

            if (innerExceptions.Any(x => x.GetType() == typeof(SocketException) && x.Message.Contains("Connection reset by peer")))
            {
                // The server has reset the connection.
                _logger.LogWarning(exception, "Socket reseted.");

                return Task.CompletedTask;
            }

            _logger.LogError(exception, exception.Message);

            // fire and forget
            _ = _githubService.CreateBugIssue($"Application Exception: {exception.Message}", exception, GithubIssueLabels.Telegram);

            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery),
                _ => UnknownUpdateHandlerAsync(update)
            };

            try
            {
                await PollyExceptionHandlerHelper.HandleExceptionAndRetry<SqlException>(handler, _logger, cancellationToken);
            }
            catch (SqlException ex) when (ex.Message.Contains("SHUTDOWN"))
            {
                _logger.LogWarning(ex, "Sql Server shutdown in progress");
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private async Task BotOnMessageReceived(ITelegramBotClient client, Message message)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            if (message.Type != MessageType.Text) // We only analyze Text messages
                return;

            _logger.LogInformation($"Message received from chat id: {message.Chat.Id}");

            var chatConfig = await GetChatConfiguration(message.Chat.Id, serviceProvider, client);

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
            await client.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            var messageBuilder = new StringBuilder();

            foreach (var correction in corretionResult.Corrections)
            {
                var correctionDetailMessage = !chatConfig.HideCorrectionDetails && !string.IsNullOrEmpty(correction.Message)
                    ? $"[{correction.Message}]"
                    : string.Empty;

                messageBuilder.AppendLine($"*{correction.PossibleReplacements.First()} {correctionDetailMessage}");
            }

            await client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString(), replyToMessageId: message.MessageId);
        }

        private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var telegramCommandHandlerService = scope.ServiceProvider.GetService<ITelegramCommandHandlerService>();

            await telegramCommandHandlerService.HandleCallBackQuery(callbackQuery);
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            _logger.LogInformation($"Unknown update type received: {update.Type}");
            return Task.CompletedTask;
        }

        private async Task<ChatConfiguration> GetChatConfiguration(long chatId, IServiceProvider serviceProvider, ITelegramBotClient client)
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
            await client.SendTextMessageAsync(chatId, messageBuilder.ToString());

            return chatConfiguration;
        }

        private static IGrammarService GetConfiguredGrammarService(ChatConfiguration chatConfig, IServiceProvider serviceProvider)
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
            var text = GetTextWithoutMentionOrSpoiler();

            return StringUtils.RemoveEmojis(StringUtils.RemoveHashtags(StringUtils.RemoveMentions(text)));

            string GetTextWithoutMentionOrSpoiler()
            {
                if (message.Entities == null)
                    return message.Text;

                var messageText = message.Text;

                var entities = message.Entities.Where(v => v.Type == MessageEntityType.TextMention
                                                // There isn't a MessageEntityType.Spoiler yet in this current version of Telegram.Bot.
                                                // So when v.Type == 0, is probably a Spoiler.
                                                // TODO: Update this when Telegram.Bot v18 is released.
                                                || v.Type == 0);

                foreach (var entity in entities)
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