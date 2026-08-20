using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using System.Text;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram;

public class SettingsCommand(IChatConfigurationService chatConfigurationService, ITelegramBotClientWrapper telegramBotClient)
    : BaseTelegramCommand(telegramBotClient), ITelegramBotCommand
{
    public string Command => TelegramBotCommands.Settings;

    public async Task Handle(Message message)
    {
        await SendTypingNotification(message);

        var chatConfig = await chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("Algorithms Available:");
        messageBuilder.AppendLine(GetAvailableOptions(chatConfig.GrammarAlgorithm));
        messageBuilder.AppendLine("Supported Languages:");
        messageBuilder.AppendLine(GetAvailableOptions(chatConfig.SelectedLanguage));

        var showCorrectionDetailsIcon = chatConfig.HideCorrectionDetails ? "❌" : "✅";
        messageBuilder.AppendLine($"Show correction details {showCorrectionDetailsIcon}").AppendLine();
        messageBuilder.AppendLine("Strictness level:").AppendLine($"{chatConfig.CorrectionStrictnessLevel.Description} ✅").AppendLine();

        messageBuilder.AppendLine($"Whitelist Words:").AppendLine($"Type {TelegramBotCommands.WhiteList} to see Whitelist words configured.").AppendLine();

        if (chatConfig.IsBotStopped)
        {
            messageBuilder.AppendLine($"The bot is currently stopped. Type {TelegramBotCommands.Start} to activate the Bot.");
        }

        await Client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
        await NotifyIfBotIsNotAdmin(message);
    }
}
