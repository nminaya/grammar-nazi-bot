using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GrammarNazi.Domain.Services
{
    public interface ITelegramCommandHandlerService
    {
        Task HandleCommand(Message message);

        Task HandleCallBackQuery(CallbackQuery callbackQuery);
    }
}