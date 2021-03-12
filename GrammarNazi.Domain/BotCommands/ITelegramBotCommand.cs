using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GrammarNazi.Domain.BotCommands
{
    public interface ITelegramBotCommand
    {
        string Command { get; }
        Task Handle(Message message);
    }
}
