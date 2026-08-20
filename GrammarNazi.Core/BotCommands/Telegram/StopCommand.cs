using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram;

public class StopCommand(IChatConfigurationService chatConfigurationService, ITelegramBotClientWrapper telegramBotClient)
    : BaseTelegramCommand(telegramBotClient), ITelegramBotCommand
{
    public string Command => TelegramBotCommands.Stop;

    public async Task Handle(Message message)
    {
        await SendTypingNotification(message);

        if (!await IsUserAdmin(message))
        {
            await Client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyParameters: message.MessageId);
            return;
        }

        var chatConfig = await chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

        chatConfig.IsBotStopped = true;

        await chatConfigurationService.Update(chatConfig);

        await Client.SendTextMessageAsync(message.Chat.Id, "Bot stopped");
    }
}
