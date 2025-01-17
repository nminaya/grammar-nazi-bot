﻿using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram;

public class ShowDetailsCommand : BaseTelegramCommand, ITelegramBotCommand
{
    private readonly IChatConfigurationService _chatConfigurationService;

    public string Command => TelegramBotCommands.ShowDetails;

    public ShowDetailsCommand(IChatConfigurationService chatConfigurationService,
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

        chatConfig.HideCorrectionDetails = false;

        await _chatConfigurationService.Update(chatConfig);

        await Client.SendTextMessageAsync(message.Chat.Id, "Show correction details ✅");

        await NotifyIfBotIsNotAdmin(message);
    }
}