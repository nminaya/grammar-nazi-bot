using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GrammarNazi.Core.Services
{
    public class TelegramCommandHandlerService : ITelegramCommandHandlerService
    {
        private readonly IChatConfigurationService _chatConfigurationService;
        private readonly ITelegramBotClient _client;

        public TelegramCommandHandlerService(IChatConfigurationService chatConfigurationService,
            ITelegramBotClient telegramBotClient)
        {
            _chatConfigurationService = chatConfigurationService;
            _client = telegramBotClient;
        }

        public async Task HandleCommand(Message message)
        {
            var text = message.Text;

            if (IsCommand(Commands.Start, text))
            {
                await SendTypingNotification(message);

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);
                var messageBuilder = new StringBuilder();

                if (chatConfig == null)
                {
                    messageBuilder.AppendLine("Hi, I'm GrammarNazi.");
                    messageBuilder.AppendLine("I'm currently working and correcting all spelling errors in this chat.");
                    messageBuilder.AppendLine($"Type {Commands.Help} to get useful commands.");

                    var chatConfiguration = new ChatConfiguration
                    {
                        ChatId = message.Chat.Id,
                        GrammarAlgorithm = Defaults.DefaultAlgorithm,
                        CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant,
                        SelectedLanguage = SupportedLanguages.Auto
                    };

                    await _chatConfigurationService.AddConfiguration(chatConfiguration);
                }
                else if (chatConfig.IsBotStopped)
                {
                    if (!await IsUserAdmin(message))
                    {
                        messageBuilder.AppendLine("Only admins can use this command.");
                    }
                    else
                    {
                        chatConfig.IsBotStopped = false;
                        await _chatConfigurationService.Update(chatConfig);
                        messageBuilder.AppendLine("Bot started");
                    }
                }
                else
                {
                    messageBuilder.AppendLine("Bot is already started");
                }

                await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.Help, text))
            {
                await SendTypingNotification(message);

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Help").AppendLine();
                messageBuilder.AppendLine("Useful commands:");
                messageBuilder.AppendLine($"{Commands.Start} start/activate the Bot.");
                messageBuilder.AppendLine($"{Commands.Stop} stop/disable the Bot.");
                messageBuilder.AppendLine($"{Commands.Settings} get configured settings.");
                messageBuilder.AppendLine($"{Commands.SetAlgorithm} <algorithm_number> to set an algorithm.");
                messageBuilder.AppendLine($"{Commands.Language} <language_number> to set a language.");
                messageBuilder.AppendLine($"{Commands.ShowDetails} Show correction details");
                messageBuilder.AppendLine($"{Commands.HideDetails} Hide correction details");
                messageBuilder.AppendLine($"{Commands.WhiteList} See list of ignored words.");
                messageBuilder.AppendLine($"{Commands.AddWhiteList} <word> to add a Whitelist word.");
                messageBuilder.AppendLine($"{Commands.RemoveWhiteList} <word> to remove a Whitelist word.");
                messageBuilder.AppendLine($"{Commands.Tolerant} Set strictness level to {CorrectionStrictnessLevels.Tolerant.GetDescription()}");
                messageBuilder.AppendLine($"{Commands.Intolerant} Set strictness level to {CorrectionStrictnessLevels.Intolerant.GetDescription()}");

                await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.Settings, text))
            {
                await SendTypingNotification(message);

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                // TODO: Investigate how this could be null at this point https://github.com/nminaya/grammar-nazi-bot/issues/56
                if (chatConfig == null)
                    return;

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(GetAvailableAlgorithms(chatConfig.GrammarAlgorithm));
                messageBuilder.AppendLine(GetSupportedLanguages(chatConfig.SelectedLanguage));

                var showCorrectionDetailsIcon = chatConfig.HideCorrectionDetails ? "❌" : "✅";
                messageBuilder.AppendLine($"Show correction details {showCorrectionDetailsIcon}").AppendLine();
                messageBuilder.AppendLine("Strictness level:").AppendLine($"{chatConfig.CorrectionStrictnessLevel.GetDescription()} ✅").AppendLine();

                messageBuilder.AppendLine($"Whitelist Words:").AppendLine($"Type {Commands.WhiteList} to see Whitelist words configured.").AppendLine();

                if (chatConfig.IsBotStopped)
                    messageBuilder.AppendLine($"The bot is currently stopped. Type {Commands.Start} to activate the Bot.");

                await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.SetAlgorithm, text))
            {
                await SendTypingNotification(message);

                var messageBuilder = new StringBuilder();

                if (!await IsUserAdmin(message))
                {
                    messageBuilder.AppendLine("Only admins can use this command.");
                    await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString(), replyToMessageId: message.MessageId);
                    return;
                }

                var parameters = text.Split(" ");
                if (parameters.Length == 1)
                {
                    await ShowAlgorithmOptions(message);
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int algorithm);

                    if (parsedOk && algorithm.IsAssignableToEnum<GrammarAlgorithms>())
                    {
                        var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);
                        chatConfig.GrammarAlgorithm = (GrammarAlgorithms)algorithm;

                        await _chatConfigurationService.Update(chatConfig);

                        await _client.SendTextMessageAsync(message.Chat.Id, "Algorithm updated.");
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(message.Chat.Id, $"Invalid parameter. Type {Commands.SetAlgorithm} <algorithm_numer> to set an algorithm.");
                    }
                }

                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.Language, text))
            {
                await SendTypingNotification(message);

                var messageBuilder = new StringBuilder();

                if (!await IsUserAdmin(message))
                {
                    messageBuilder.AppendLine("Only admins can use this command.");
                    await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString(), replyToMessageId: message.MessageId);
                    return;
                }

                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    await ShowLanguageOptions(message);
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int language);

                    if (parsedOk && language.IsAssignableToEnum<SupportedLanguages>())
                    {
                        var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);
                        chatConfig.SelectedLanguage = (SupportedLanguages)language;

                        await _chatConfigurationService.Update(chatConfig);

                        await _client.SendTextMessageAsync(message.Chat.Id, "Language updated.");
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(message.Chat.Id, $"Invalid parameter. Type {Commands.Language} <language_number> to set a language.");
                    }
                }

                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.Stop, text))
            {
                await SendTypingNotification(message);

                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.IsBotStopped = true;

                await _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, "Bot stopped");
            }
            else if (IsCommand(Commands.HideDetails, text))
            {
                await SendTypingNotification(message);

                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.HideCorrectionDetails = true;

                await _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, "Correction details hidden ✅");

                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.ShowDetails, text))
            {
                await SendTypingNotification(message);

                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.HideCorrectionDetails = false;

                await _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, "Show correction details ✅");

                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.Tolerant, text))
            {
                await SendTypingNotification(message);

                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Tolerant;

                await _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, "Tolerant ✅");

                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.Intolerant, text))
            {
                await SendTypingNotification(message);

                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant;

                await _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, "Intolerant ✅");

                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.WhiteList, text))
            {
                await SendTypingNotification(message);

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                if (chatConfig.WhiteListWords?.Any() == true)
                {
                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine("Whitelist Words:\n");

                    foreach (var word in chatConfig.WhiteListWords)
                    {
                        messageBuilder.AppendLine($"- {word}");
                    }

                    await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());

                    return;
                }

                await _client.SendTextMessageAsync(message.Chat.Id, $"You don't have Whitelist words configured. Use {Commands.AddWhiteList} to add words to the WhiteList.");

                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.AddWhiteList, text))
            {
                await SendTypingNotification(message);

                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, $"Parameter not received. Type {Commands.AddWhiteList} <word> to add a Whitelist word.");
                }
                else
                {
                    var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                    var word = parameters[1].Trim();

                    if (chatConfig.WhiteListWords.Contains(word))
                    {
                        await _client.SendTextMessageAsync(message.Chat.Id, $"The word '{word}' is already on the WhiteList");
                        return;
                    }

                    chatConfig.WhiteListWords.Add(word);

                    await _chatConfigurationService.Update(chatConfig);

                    await _client.SendTextMessageAsync(message.Chat.Id, $"Word '{word}' added to the WhiteList.");
                }

                await NotifyIfBotIsNotAdmin(message);
            }
            else if (IsCommand(Commands.RemoveWhiteList, text))
            {
                await SendTypingNotification(message);

                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, $"Parameter not received. Type {Commands.RemoveWhiteList} <word> to remove a Whitelist word.");
                }
                else
                {
                    var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                    var word = parameters[1].Trim();

                    if (!chatConfig.WhiteListWords.Contains(word))
                    {
                        await _client.SendTextMessageAsync(message.Chat.Id, $"The word '{word}' is not in the WhiteList.");
                        return;
                    }

                    chatConfig.WhiteListWords.Remove(word);

                    await _chatConfigurationService.Update(chatConfig);

                    await _client.SendTextMessageAsync(message.Chat.Id, $"Word '{word}' removed from the WhiteList.");
                }

                await NotifyIfBotIsNotAdmin(message);
            }
        }

        public async Task HandleCallBackQuery(CallbackQuery callbackQuery)
        {
            var message = callbackQuery.Message;

            if (!await IsUserAdmin(callbackQuery.Message, callbackQuery.From))
            {
                var userMention = $"[{callbackQuery.From.FirstName} {callbackQuery.From.LastName}](tg://user?id={callbackQuery.From.Id})";

                await _client.SendTextMessageAsync(message.Chat.Id, $"{userMention} Only admins can use this command.", ParseMode.Markdown);
                return;
            }

            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

            var enumTypeString = callbackQuery.Data.Split(".")[0];

            if (enumTypeString == nameof(SupportedLanguages))
            {
                var languageSelectedString = callbackQuery.Data.Split(".")[1];

                var languageSelected = Enum.GetValues(typeof(SupportedLanguages)).Cast<SupportedLanguages>().First(v => v.ToString() == languageSelectedString);

                chatConfig.SelectedLanguage = languageSelected;

                await _client.SendTextMessageAsync(message.Chat.Id, $"Language updated: {languageSelected}");
            }
            else
            {
                var algorithmSelectedString = callbackQuery.Data.Split(".")[1];

                var algorithmSelected = Enum.GetValues(typeof(GrammarAlgorithms)).Cast<GrammarAlgorithms>().First(v => v.ToString() == algorithmSelectedString);

                chatConfig.GrammarAlgorithm = algorithmSelected;

                await _client.SendTextMessageAsync(message.Chat.Id, $"Algorithm updated: {algorithmSelected}");
            }

            await _chatConfigurationService.Update(chatConfig);

            await _client.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
        }

        private async Task ShowLanguageOptions(Message message)
        {
            var languages = Enum.GetValues(typeof(SupportedLanguages))
                            .Cast<SupportedLanguages>()
                            .Select(v => new[] { InlineKeyboardButton.WithCallbackData($"{(int)v} - {v}", $"{nameof(SupportedLanguages)}.{v}") });

            var inlineLanguages = new InlineKeyboardMarkup(languages);

            await _client.SendTextMessageAsync(message.Chat.Id, "Chose language", replyMarkup: inlineLanguages);
        }

        private async Task ShowAlgorithmOptions(Message message)
        {
            var languages = Enum.GetValues(typeof(GrammarAlgorithms))
                            .Cast<GrammarAlgorithms>()
                            .Select(v => new[] { InlineKeyboardButton.WithCallbackData($"{(int)v} - {v}", $"{nameof(GrammarAlgorithms)}.{v}") });

            var inlineLanguages = new InlineKeyboardMarkup(languages);

            await _client.SendTextMessageAsync(message.Chat.Id, "Chose Algorithm", replyMarkup: inlineLanguages);
        }

        private static bool IsCommand(string expected, string actual)
        {
            if (actual.Contains("@"))
            {
                return actual.StartsWith($"{expected}@{Defaults.TelegramBotUser}")
                    // For test enviroment
                    || actual.StartsWith($"{expected}@grammarNaziTest_Bot");
            }

            return actual.StartsWith(expected);
        }

        private async Task<bool> IsUserAdmin(Message message, User user = null)
        {
            if (message.Chat.Type == ChatType.Private)
                return true;

            var chatAdministrators = await _client.GetChatAdministratorsAsync(message.Chat.Id);
            var currentUserId = user == null ? message.From.Id : user.Id;

            return chatAdministrators.Any(v => v.User.Id == currentUserId);
        }

        private async Task<bool> IsBotAdmin(Message message)
        {
            if (message.Chat.Type == ChatType.Private)
                return true;

            var bot = await _client.GetMeAsync();
            var chatAdministrators = await _client.GetChatAdministratorsAsync(message.Chat.Id);

            return chatAdministrators.Any(v => v.User.Id == bot.Id);
        }

        private async Task NotifyIfBotIsNotAdmin(Message message)
        {
            if (!await IsBotAdmin(message))
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "NOTE: The bot needs admin rights in order to read messages from this chat.");
            }
        }

        private async Task SendTypingNotification(Message message)
        {
            await _client.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
        }

        private static string GetAvailableAlgorithms(GrammarAlgorithms selectedAlgorith)
        {
            var algorithms = Enum.GetValues(typeof(GrammarAlgorithms)).Cast<GrammarAlgorithms>();

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Algorithms available:");

            foreach (var item in algorithms)
            {
                var selected = item == selectedAlgorith ? "✅" : "";
                messageBuilder.AppendLine($"{(int)item} - {item.GetDescription()} {selected}");
            }

            return messageBuilder.ToString();
        }

        private static string GetSupportedLanguages(SupportedLanguages selectedLanguage)
        {
            var languages = Enum.GetValues(typeof(SupportedLanguages)).Cast<SupportedLanguages>();

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Supported Languages:");

            foreach (var item in languages)
            {
                var selected = item == selectedLanguage ? "✅" : "";
                messageBuilder.AppendLine($"{(int)item} - {item} {selected}");
            }

            return messageBuilder.ToString();
        }
    }
}