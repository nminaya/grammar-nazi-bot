using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram;

public class AddWhiteListCommand(IChatConfigurationService chatConfigurationService, ITelegramBotClientWrapper telegramBotClient)
    : BaseTelegramCommand(telegramBotClient), ITelegramBotCommand
{
    public string Command => TelegramBotCommands.AddWhiteList;

    public async Task Handle(Message message)
    {
        await SendTypingNotification(message);

        if (!await IsUserAdmin(message))
        {
            await Client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyParameters: message.MessageId);
            return;
        }

        var parameters = message.Text.Split(" ");

        if (parameters.Length == 1)
        {
            await Client.SendTextMessageAsync(message.Chat.Id, $"Parameter not received. Type {TelegramBotCommands.AddWhiteList} <word> to add a Whitelist word.");
        }
        else
        {
            var chatConfig = await chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

            var word = parameters[1].Trim();

            if (chatConfig.WhiteListWords.Contains(word, StringComparer.OrdinalIgnoreCase))
            {
                await Client.SendTextMessageAsync(message.Chat.Id, $"The word '{word}' is already on the WhiteList");
                return;
            }

            chatConfig.WhiteListWords.Add(word);

            await chatConfigurationService.Update(chatConfig);

            await Client.SendTextMessageAsync(message.Chat.Id, $"Word '{word}' added to the WhiteList.");
        }

        await NotifyIfBotIsNotAdmin(message);
    }
}
