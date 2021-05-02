using Telegram.Bot.Types;

namespace GrammarNazi.Domain.BotCommands
{
    public interface ITelegramBotCommand : IBotCommand<Message>
    {
    }
}