using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System;

namespace GrammarNazi.Core.BotCommands.Discord;

public class RemoveWhiteListCommand(IDiscordChannelConfigService channelConfigService) : BaseDiscordCommand, IDiscordBotCommand
{
    public string Command => DiscordBotCommands.RemoveWhiteList;

    public async Task Handle(IMessage message)
    {
        if (!IsUserAdmin(message))
        {
            await message.Channel.SendMessageAsync("Only admins can use this command.", messageReference: new MessageReference(message.Id));
            return;
        }

        var parameters = message.Content.Split(" ");

        if (parameters.Length == 1)
        {
            await SendMessage(message, $"Parameter not received. Type `{DiscordBotCommands.RemoveWhiteList}` <word> to remove a Whitelist word.", DiscordBotCommands.RemoveWhiteList);
            return;
        }

        var channelConfig = await channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

        var word = parameters[1].Trim();

        if (!channelConfig.WhiteListWords.Contains(word, StringComparer.OrdinalIgnoreCase))
        {
            await SendMessage(message, $"The word '{word}' is not in the WhiteList.", DiscordBotCommands.RemoveWhiteList);
            return;
        }

        channelConfig.WhiteListWords.RemoveAll(v => v.Equals(word, StringComparison.OrdinalIgnoreCase));

        await channelConfigService.Update(channelConfig);

        await SendMessage(message, $"Word '{word}' removed from the WhiteList.", DiscordBotCommands.RemoveWhiteList);
    }
}
