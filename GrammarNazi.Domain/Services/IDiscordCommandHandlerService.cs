using Discord.WebSocket;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface IDiscordCommandHandlerService
    {
        Task HandleCommand(SocketUserMessage message);
    }
}