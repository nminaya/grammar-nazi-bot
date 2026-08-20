using Discord;

namespace GrammarNazi.Domain.Services;

public interface IDiscordCommandHandlerService
{
    Task HandleCommand(IMessage message);
}
