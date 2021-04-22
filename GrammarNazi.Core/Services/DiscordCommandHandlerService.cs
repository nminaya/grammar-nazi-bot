using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class DiscordCommandHandlerService : IDiscordCommandHandlerService
    {
        private readonly IEnumerable<IDiscordBotCommand> _botCommands;

        public DiscordCommandHandlerService(IEnumerable<IDiscordBotCommand> botCommands)
        {
            _botCommands = botCommands;
        }

        public async Task HandleCommand(IMessage message)
        {
            var command = _botCommands.FirstOrDefault(v => message.Content.StartsWith(v.Command));

            if (command != null)
            {
                await command.Handle(message);
            }
        }
    }
}