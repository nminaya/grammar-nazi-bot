using Discord;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord;

public abstract class BaseDiscordCommand
{
    protected bool IsUserAdmin(IMessage message)
    {
        if (message.Channel is IPrivateChannel)
        {
            return true;
        }

        var user = message.Author as IGuildUser;
        return user.GuildPermissions.Administrator || user.GuildPermissions.ManageChannels;
    }

    protected async Task SendMessage(IMessage socketUserMessage, string message, string command)
    {
        var embed = new EmbedBuilder
        {
            Color = new Color(194, 12, 60),
            Title = command,
            Description = message
        };

        await socketUserMessage.Channel.SendMessageAsync(embed: embed.Build());
    }

    protected string GetAvailableOptions<T>(T selectedOption) where T : struct, Enum
    {
        return EnumUtils.GetAvailableOptions(selectedOption);
    }

    protected async Task SendWarningMessageIfLanguageNotSupported(IMessage message, string command, SupportedLanguages language, GrammarAlgorithms algorithm)
    {
        if (language == SupportedLanguages.Auto)
        {
            return;
        }

        if (algorithm.IsLanguageSupported(language))
        {
            return;
        }

        await SendMessage(message, EnumUtils.GetUnsupportedLanguageWarning(language, algorithm), command);
    }
}
