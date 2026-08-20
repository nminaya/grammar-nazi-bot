using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using System.Text;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram;

public class WhiteListCommand(IChatConfigurationService chatConfigurationService, ITelegramBotClientWrapper telegramBotClient)
    : BaseTelegramCommand(telegramBotClient), ITelegramBotCommand
{
    public string Command => TelegramBotCommands.WhiteList;

    public async Task Handle(Message message)
    {
        await SendTypingNotification(message);

        var chatConfig = await chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

        if (chatConfig.WhiteListWords?.Any() == true)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Whitelist Words:\n");

            foreach (var word in chatConfig.WhiteListWords)
            {
                messageBuilder.AppendLine($"- {word}");
            }

            await Client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());

            return;
        }

        await Client.SendTextMessageAsync(message.Chat.Id, $"You don't have Whitelist words configured. Use {TelegramBotCommands.AddWhiteList} to add words to the WhiteList.");

        await NotifyIfBotIsNotAdmin(message);
    }
}
