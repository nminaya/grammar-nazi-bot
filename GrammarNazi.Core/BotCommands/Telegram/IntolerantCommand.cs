using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram;

public class IntolerantCommand(IChatConfigurationService chatConfigurationService, ITelegramBotClientWrapper telegramBotClient)
    : BaseTelegramCommand(telegramBotClient), ITelegramBotCommand
{
    public string Command => TelegramBotCommands.Intolerant;

    public async Task Handle(Message message)
    {
        await SendTypingNotification(message);

        if (!await IsUserAdmin(message))
        {
            await Client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyParameters: message.MessageId);
            return;
        }

        var chatConfig = await chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

        chatConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant;

        await chatConfigurationService.Update(chatConfig);

        await Client.SendTextMessageAsync(message.Chat.Id, "Intolerant ✅");

        await NotifyIfBotIsNotAdmin(message);
    }
}
