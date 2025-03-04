using GrammarNazi.Domain.Utilities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GrammarNazi.Core.Utilities;

public class TelegramBotClientWrapper(ITelegramBotClient client) : ITelegramBotClientWrapper
{
    public Task DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
    {
        return client.DeleteMessage(chatId, messageId, cancellationToken: cancellationToken);
    }

    public Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return client.GetChatAdministrators(chatId, cancellationToken: cancellationToken);
    }

    public Task<User> GetMeAsync(CancellationToken cancellationToken = default)
    {
        return client.GetMe(cancellationToken);
    }

    public Task SendChatActionAsync(ChatId chatId, ChatAction chatAction, CancellationToken cancellationToken = default)
    {
        return client.SendChatAction(chatId, chatAction, cancellationToken: cancellationToken);
    }

    public Task<Message> SendTextMessageAsync(ChatId chatId, string text, ParseMode parseMode = default,
        IEnumerable<MessageEntity> entities = null, bool? linkPreviewOptions = null, bool disableNotification = false,
        bool protectContent = false, int? replyParameters = null, ReplyMarkup replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return client.SendMessage(chatId,
            text,
            parseMode: parseMode,
            entities: entities,
            linkPreviewOptions: linkPreviewOptions,
            disableNotification: disableNotification,
            protectContent: protectContent,
            replyParameters: replyParameters,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }
}