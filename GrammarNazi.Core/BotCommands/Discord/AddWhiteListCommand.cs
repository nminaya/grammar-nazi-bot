﻿using Discord;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord;

public class AddWhiteListCommand : BaseDiscordCommand, IDiscordBotCommand
{
    private readonly IDiscordChannelConfigService _channelConfigService;

    public string Command => DiscordBotCommands.AddWhiteList;

    public AddWhiteListCommand(IDiscordChannelConfigService discordChannelConfigService)
    {
        _channelConfigService = discordChannelConfigService;
    }

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
            await SendMessage(message, $"Parameter not received. Type `{DiscordBotCommands.AddWhiteList}` <word> to add a Whitelist word.", DiscordBotCommands.AddWhiteList);
            return;
        }

        var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

        var word = parameters[1].Trim();

        if (channelConfig.WhiteListWords.Contains(word, new CaseInsensitiveEqualityComparer()))
        {
            await SendMessage(message, $"The word '{word}' is already on the WhiteList", DiscordBotCommands.AddWhiteList);
            return;
        }

        channelConfig.WhiteListWords.Add(word);

        await _channelConfigService.Update(channelConfig);

        await SendMessage(message, $"Word '{word}' added to the WhiteList.", DiscordBotCommands.AddWhiteList);
    }
}