using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram;

public class HideDetailsCommand : BaseTelegramCommand, ITelegramBotCommand
{
    private readonly IChatConfigurationService _chatConfigurationService;

    public string Command => TelegramBotCommands.HideDetails;

    public HideDetailsCommand(IChatConfigurationService chatConfigurationService,
        ITelegramBotClientWrapper telegramBotClient)
        : base(telegramBotClient)
    {
        _chatConfigurationService = chatConfigurationService;
    }

    public async Task Handle(Message message)
    {
        await SendTypingNotification(message);

        if (!await IsUserAdmin(message))
        {
            await Client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyParameters: message.MessageId);
            return;
        }

        var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

        chatConfig.HideCorrectionDetails = true;

        await _chatConfigurationService.Update(chatConfig);

        await Client.SendTextMessageAsync(message.Chat.Id, "Correction details hidden ✅");

        await NotifyIfBotIsNotAdmin(message);
    }
}