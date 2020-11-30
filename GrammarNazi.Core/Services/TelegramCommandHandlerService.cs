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
                else
                {
                    if (chatConfig.IsBotStopped)
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
                }

                await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
            }
            else if (IsCommand(Commands.Help, text))
            {
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
            }
            else if (IsCommand(Commands.Settings, text))
            {
                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(GetAvailableAlgorithms(chatConfig.GrammarAlgorithm));
                messageBuilder.AppendLine(GetSupportedLanguages(chatConfig.SelectedLanguage));
                messageBuilder.AppendLine($"WhiteList words:").AppendLine($"Type {Commands.WhiteList} to see Whitelist words configured.").AppendLine();

                var showCorrectionDetailsIcon = chatConfig.HideCorrectionDetails ? "❌" : "✅";
                messageBuilder.AppendLine($"Show correction details {showCorrectionDetailsIcon}").AppendLine();
                messageBuilder.AppendLine("Strictness level:").AppendLine($"{chatConfig.CorrectionStrictnessLevel.GetDescription()} ✅").AppendLine();

                if (chatConfig.IsBotStopped)
                    messageBuilder.AppendLine($"The bot is currently stopped. Type {Commands.Start} to activate the Bot.");

                await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
            }
            else if (IsCommand(Commands.SetAlgorithm, text))
            {
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
                    var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                    messageBuilder.AppendLine($"Parameter not received. Type {Commands.SetAlgorithm} <algorithm_numer> to set an algorithm").AppendLine();
                    messageBuilder.AppendLine(GetAvailableAlgorithms(chatConfig.GrammarAlgorithm));
                    await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int algorithm);

                    if (parsedOk && algorithm.IsAssignableToEnum<GrammarAlgorithms>())
                    {
                        var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);
                        chatConfig.GrammarAlgorithm = (GrammarAlgorithms)algorithm;

                        // Fire and forget
                        _ = _chatConfigurationService.Update(chatConfig);

                        await _client.SendTextMessageAsync(message.Chat.Id, "Algorithm updated.");
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(message.Chat.Id, $"Invalid parameter. Type {Commands.SetAlgorithm} <algorithm_numer> to set an algorithm.");
                    }
                }
            }
            else if (IsCommand(Commands.Language, text))
            {
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
                    var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                    messageBuilder.AppendLine($"Parameter not received. Type {Commands.Language} <language_number> to set a language.").AppendLine();
                    messageBuilder.AppendLine(GetSupportedLanguages(chatConfig.SelectedLanguage));
                    await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int language);

                    if (parsedOk && language.IsAssignableToEnum<SupportedLanguages>())
                    {
                        var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);
                        chatConfig.SelectedLanguage = (SupportedLanguages)language;

                        // Fire and forget
                        _ = _chatConfigurationService.Update(chatConfig);

                        await _client.SendTextMessageAsync(message.Chat.Id, "Language updated.");
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(message.Chat.Id, $"Invalid parameter. Type {Commands.Language} <language_number> to set a language.");
                    }
                }
            }
            else if (IsCommand(Commands.Stop, text))
            {
                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.IsBotStopped = true;

                // Fire and forget
                _ = _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, $"Bot stopped");
            }
            else if (IsCommand(Commands.HideDetails, text))
            {
                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.HideCorrectionDetails = true;

                // Fire and forget
                _ = _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, "Correction details hidden ✅");
            }
            else if (IsCommand(Commands.ShowDetails, text))
            {
                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.HideCorrectionDetails = false;

                // Fire and forget
                _ = _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, "Show correction details ✅");
            }
            else if (IsCommand(Commands.Tolerant, text))
            {
                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Tolerant;

                // Fire and forget
                _ = _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, "Tolerant ✅");
            }
            else if (IsCommand(Commands.Intolerant, text))
            {
                if (!await IsUserAdmin(message))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                    return;
                }

                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                chatConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant;

                // Fire and forget
                _ = _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, "Intolerant ✅");
            }
            else if (IsCommand(Commands.WhiteList, text))
            {
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
            }
            else if (IsCommand(Commands.AddWhiteList, text))
            {
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

                    _ = _chatConfigurationService.Update(chatConfig);

                    await _client.SendTextMessageAsync(message.Chat.Id, $"Word '{word}' added to the WhiteList.");
                }
            }
            else if (IsCommand(Commands.RemoveWhiteList, text))
            {
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

                    _ = _chatConfigurationService.Update(chatConfig);

                    await _client.SendTextMessageAsync(message.Chat.Id, $"Word '{word}' removed from the WhiteList.");
                }
            }
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

        private async Task<bool> IsUserAdmin(Message message)
        {
            if (message.Chat.Type == ChatType.Private)
                return true;

            var chatAdministrators = await _client.GetChatAdministratorsAsync(message.Chat.Id);
            var currentUserId = message.From.Id;

            return chatAdministrators.Any(v => v.User.Id == currentUserId);
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