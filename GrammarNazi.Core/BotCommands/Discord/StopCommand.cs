using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;

namespace GrammarNazi.Core.BotCommands.Discord;

public class StopCommand(IDiscordChannelConfigService channelConfigService) : BaseDiscordCommand, IDiscordBotCommand
{
    public string Command => DiscordBotCommands.Stop;

    public async Task Handle(IMessage message)
    {
        if (!IsUserAdmin(message))
        {
            await message.Channel.SendMessageAsync("Only admins can use this command.", messageReference: new MessageReference(message.Id));
            return;
        }

        var channelConfig = await channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

        channelConfig.IsBotStopped = true;

        await channelConfigService.Update(channelConfig);

        await SendMessage(message, "Bot stopped", DiscordBotCommands.Stop);
    }
}
