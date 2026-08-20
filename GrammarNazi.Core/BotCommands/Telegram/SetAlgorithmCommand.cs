using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using System.Text;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram;

public class SetAlgorithmCommand(IChatConfigurationService chatConfigurationService, ITelegramBotClientWrapper telegramBotClient)
    : BaseTelegramCommand(telegramBotClient), ITelegramBotCommand
{
    public string Command => TelegramBotCommands.SetAlgorithm;

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
            await ShowOptions<GrammarAlgorithms>(message, "Choose Algorithm");
        }
        else
        {
            bool parsedOk = int.TryParse(parameters[1], out int algorithm);

            if (parsedOk && algorithm.IsAssignableToEnum<GrammarAlgorithms>() && !((GrammarAlgorithms)algorithm).IsDisabled)
            {
                var chatConfig = await chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);
                chatConfig.GrammarAlgorithm = (GrammarAlgorithms)algorithm;

                await chatConfigurationService.Update(chatConfig);

                await Client.SendTextMessageAsync(message.Chat.Id, "Algorithm updated.");
                await SendWarningMessageIfLanguageNotSupported(message, chatConfig.SelectedLanguage, chatConfig.GrammarAlgorithm);
            }
            else
            {
                await Client.SendTextMessageAsync(message.Chat.Id, $"Invalid parameter. Type {TelegramBotCommands.SetAlgorithm} <algorithm_numer> to set an algorithm.");
            }
        }

        await NotifyIfBotIsNotAdmin(message);
    }
}
