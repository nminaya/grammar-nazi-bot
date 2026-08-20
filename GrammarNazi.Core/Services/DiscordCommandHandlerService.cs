using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Services;

namespace GrammarNazi.Core.Services;

public class DiscordCommandHandlerService(IEnumerable<IDiscordBotCommand> botCommands) : IDiscordCommandHandlerService
{
    public async Task HandleCommand(IMessage message)
    {
        var command = botCommands.FirstOrDefault(v => message.Content.StartsWith(v.Command));

        if (command != null)
        {
            await command.Handle(message);
        }
    }
}
