using System.Threading.Tasks;

namespace GrammarNazi.Domain.BotCommands
{
    public interface IBotCommand<T>
    {
        Task Handle(T message);
    }
}