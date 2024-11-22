using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GrammarNazi.Domain.Utilities;

public interface ITelegramBotClientWrapper
{
    Task<Message> SendTextMessageAsync(ChatId chatId, string text, ParseMode parseMode = default, IEnumerable<MessageEntity> entities = null, bool? linkPreviewOptions = null, bool disableNotification = false, bool protectContent = false, int? replyParameters = null, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default);

    Task DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default);

    Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId, CancellationToken cancellationToken = default);

    Task<User> GetMeAsync(CancellationToken cancellationToken = default);

    Task SendChatActionAsync(ChatId chatId, ChatAction chatAction, CancellationToken cancellationToken = default);
}
