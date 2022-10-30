using GrammarNazi.Domain.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GrammarNazi.Core.Utilities;

internal static class TelegramBotHelper
{
    public static async Task<bool> IsUserAdmin(ITelegramBotClientWrapper client, Message message, User user = null)
    {
        if (message.Chat.Type == ChatType.Private)
        {
            return true;
        }

        var chatAdministrators = await client.GetChatAdministratorsAsync(message.Chat.Id);
        var currentUserId = user?.Id ?? message.From.Id;

        return chatAdministrators.Any(v => v.User.Id == currentUserId);
    }
}