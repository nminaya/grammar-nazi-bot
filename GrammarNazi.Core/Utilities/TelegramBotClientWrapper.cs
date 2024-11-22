using GrammarNazi.Domain.Utilities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GrammarNazi.Core.Utilities;

public class TelegramBotClientWrapper : ITelegramBotClientWrapper
{
    private readonly ITelegramBotClient _client;

    public TelegramBotClientWrapper(ITelegramBotClient client)
    {
        _client = client;
    }

    public Task DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
    {
        return _client.DeleteMessageAsync(chatId, messageId, cancellationToken: cancellationToken);
    }

    public Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return _client.GetChatAdministratorsAsync(chatId, cancellationToken: cancellationToken);
    }

    public Task<User> GetMeAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetMeAsync(cancellationToken);
    }

    public Task SendChatActionAsync(ChatId chatId, ChatAction chatAction, CancellationToken cancellationToken = default)
    {
        return _client.SendChatActionAsync(chatId,chatAction, cancellationToken: cancellationToken);
    }

    public Task<Message> SendTextMessageAsync(ChatId chatId, string text, ParseMode parseMode = default, IEnumerable<MessageEntity> entities = null, bool? linkPreviewOptions = null, bool disableNotification = false, bool protectContent = false, int? replyParameters = null, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default)
    {
        return _client.SendTextMessageAsync(chatId,
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