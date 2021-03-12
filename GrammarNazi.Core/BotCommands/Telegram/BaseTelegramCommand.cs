using GrammarNazi.Core.Extensions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GrammarNazi.Core.BotCommands.Telegram
{
    public abstract class BaseTelegramCommand
    {
        protected static async Task<bool> IsUserAdmin(ITelegramBotClient telegramBotClient, Message message, User user = null)
        {
            if (message.Chat.Type == ChatType.Private)
                return true;

            var chatAdministrators = await telegramBotClient.GetChatAdministratorsAsync(message.Chat.Id);
            var currentUserId = user?.Id ?? message.From.Id;

            return chatAdministrators.Any(v => v.User.Id == currentUserId);
        }

        protected static async Task<bool> IsBotAdmin(ITelegramBotClient telegramBotClient, Message message)
        {
            if (message.Chat.Type == ChatType.Private)
                return true;

            var bot = await telegramBotClient.GetMeAsync();
            var chatAdministrators = await telegramBotClient.GetChatAdministratorsAsync(message.Chat.Id);

            return chatAdministrators.Any(v => v.User.Id == bot.Id);
        }

        protected static async Task NotifyIfBotIsNotAdmin(ITelegramBotClient telegramBotClient, Message message)
        {
            if (!await IsBotAdmin(telegramBotClient, message))
            {
                await telegramBotClient.SendTextMessageAsync(message.Chat.Id, "NOTE: The bot needs admin rights in order to read messages from this chat.");
            }
        }

        protected static async Task SendTypingNotification(ITelegramBotClient telegramBotClient, Message message)
        {
            await telegramBotClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
        }

        protected static string GetAvailableOptions<T>(T selectedOption) where T : Enum
        {
            var options = Enum.GetValues(typeof(T)).Cast<T>();

            var messageBuilder = new StringBuilder();

            foreach (var item in options)
            {
                var selected = item.Equals(selectedOption) ? "✅" : "";
                messageBuilder.AppendLine($"{Convert.ToInt32(item)} - {item.GetDescription()} {selected}");
            }

            return messageBuilder.ToString();
        }
    }
}
