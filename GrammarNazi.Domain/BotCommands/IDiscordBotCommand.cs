using Discord;

namespace GrammarNazi.Domain.BotCommands
{
    public interface IDiscordBotCommand : IBotCommand<IMessage>
    {
        string Command { get; }
    }
}