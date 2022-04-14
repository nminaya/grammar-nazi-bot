using System.Threading.Tasks;

namespace GrammarNazi.Domain.BotCommands;

public interface IBotCommand<T>
{
    string Command { get; }

    Task Handle(T message);
}