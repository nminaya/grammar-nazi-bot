using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord;

public class TolerantCommand : BaseDiscordCommand, IDiscordBotCommand
{
    private readonly IDiscordChannelConfigService _channelConfigService;

    public string Command => DiscordBotCommands.Tolerant;

    public TolerantCommand(IDiscordChannelConfigService discordChannelConfigService)
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

        var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

        channelConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Tolerant;

        await _channelConfigService.Update(channelConfig);

        await SendMessage(message, "Tolerant ✅", DiscordBotCommands.Tolerant);
    }
}