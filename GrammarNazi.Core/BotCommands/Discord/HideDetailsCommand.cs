using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;

namespace GrammarNazi.Core.BotCommands.Discord;

public class HideDetailsCommand(IDiscordChannelConfigService channelConfigService) : BaseDiscordCommand, IDiscordBotCommand
{
    public string Command => DiscordBotCommands.HideDetails;

    public async Task Handle(IMessage message)
    {
        if (!IsUserAdmin(message))
        {
            await message.Channel.SendMessageAsync("Only admins can use this command.", messageReference: new MessageReference(message.Id));
            return;
        }

        var channelConfig = await channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

        channelConfig.HideCorrectionDetails = true;

        await channelConfigService.Update(channelConfig);

        await SendMessage(message, "Correction details hidden ✅", DiscordBotCommands.HideDetails);
    }
}
