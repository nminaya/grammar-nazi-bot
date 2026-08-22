using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static GrammarNazi.Core.Utilities.TelegramBotHelper;

namespace GrammarNazi.Core.Services;

public class TelegramCommandHandlerService(IChatConfigurationService chatConfigurationService,
    ITelegramBotClientWrapper telegramBotClient,
    IEnumerable<ITelegramBotCommand> botCommands) : ITelegramCommandHandlerService
{
    public async Task HandleCommand(Message message)
    {
        var command = botCommands.FirstOrDefault(v => IsCommand(v.Command, message.Text));

        if (command != null)
        {
            await command.Handle(message);
        }
    }

    public async Task HandleCallBackQuery(CallbackQuery callbackQuery)
    {
        var message = callbackQuery.Message;

        if (!await IsUserAdmin(telegramBotClient, callbackQuery.Message, callbackQuery.From))
        {
            var userMention = $"[{callbackQuery.From.FirstName} {callbackQuery.From.LastName}](tg://user?id={callbackQuery.From.Id})";

            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, $"{userMention} Only admins can use this command.", ParseMode.Markdown);
            return;
        }

        var chatConfig = await chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

        var enumTypeString = callbackQuery.Data.Split(".")[0];

        if (enumTypeString == nameof(SupportedLanguages))
        {
            var languageSelectedString = callbackQuery.Data.Split(".")[1];

            if (!Enum.TryParse<SupportedLanguages>(languageSelectedString, out var languageSelected))
            {
                return;
            }

            chatConfig.SelectedLanguage = languageSelected;

            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, $"Language updated: {languageSelected.Description}");
        }
        else
        {
            var algorithmSelectedString = callbackQuery.Data.Split(".")[1];

            if (!Enum.TryParse<GrammarAlgorithms>(algorithmSelectedString, out var algorithmSelected))
            {
                return;
            }

            chatConfig.GrammarAlgorithm = algorithmSelected;

            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, $"Algorithm updated: {algorithmSelected.Description}");
        }

        await chatConfigurationService.Update(chatConfig);
        await SendWarningMessageIfLanguageNotSupported(message, chatConfig.SelectedLanguage, chatConfig.GrammarAlgorithm);

        // Fire and forget
        _ = telegramBotClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
    }

    private async Task SendWarningMessageIfLanguageNotSupported(Message message, SupportedLanguages language, GrammarAlgorithms algorithm)
    {
        if (language == SupportedLanguages.Auto)
        {
            return;
        }

        if (algorithm.IsLanguageSupported(language))
        {
            return;
        }

        await telegramBotClient.SendTextMessageAsync(message.Chat.Id, EnumUtils.GetUnsupportedLanguageWarning(language, algorithm));
    }

    private static bool IsCommand(string expected, string actual)
    {
        if (actual.Contains("@"))
        {
            return actual.StartsWith($"{expected}@{Defaults.TelegramBotUser}")
                // For test environment
                || actual.StartsWith($"{expected}@grammarNaziTest_Bot");
        }

        return actual.StartsWith(expected);
    }
}
