using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.BotCommands
{
    public interface IDiscordBotCommand
    {
        string Command { get; }

        Task Handle(IMessage message);
    }
}