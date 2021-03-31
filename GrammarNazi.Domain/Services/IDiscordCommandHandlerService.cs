using Discord;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface IDiscordCommandHandlerService
    {
        Task HandleCommand(IMessage message);
    }
}