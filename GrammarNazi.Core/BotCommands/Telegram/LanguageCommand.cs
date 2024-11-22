using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram;

public class LanguageCommand : BaseTelegramCommand, ITelegramBotCommand
{
    private readonly IChatConfigurationService _chatConfigurationService;

    public string Command => TelegramBotCommands.Language;

    public LanguageCommand(IChatConfigurationService chatConfigurationService,
        ITelegramBotClientWrapper telegramBotClient)
        : base(telegramBotClient)
    {
        _chatConfigurationService = chatConfigurationService;
    }

    public async Task Handle(Message message)
    {
        await SendTypingNotification(message);

        var messageBuilder = new StringBuilder();

        if (!await IsUserAdmin(message))
        {
            messageBuilder.AppendLine("Only admins can use this command.");
            await Client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString(), replyParameters: message.MessageId);
            return;
        }

        var parameters = message.Text.Split(" ");

        if (parameters.Length == 1)
        {
            await ShowOptions<SupportedLanguages>(message, "Choose Language");
        }
        else
        {
            bool parsedOk = int.TryParse(parameters[1], out int language);

            if (parsedOk && language.IsAssignableToEnum<SupportedLanguages>())
            {
                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);
                chatConfig.SelectedLanguage = (SupportedLanguages)language;

                await _chatConfigurationService.Update(chatConfig);

                await Client.SendTextMessageAsync(message.Chat.Id, "Language updated.");

                await SendWarningMessageIfLanguageNotSupported(message, chatConfig.SelectedLanguage, chatConfig.GrammarAlgorithm);
            }
            else
            {
                await Client.SendTextMessageAsync(message.Chat.Id, $"Invalid parameter. Type {TelegramBotCommands.Language} <language_number> to set a language.");
            }
        }

        await NotifyIfBotIsNotAdmin(message);
    }
}