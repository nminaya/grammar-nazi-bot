using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Text;

namespace GrammarNazi.Core.BotCommands.Discord;

public class StartCommand(IDiscordChannelConfigService channelConfigService) : BaseDiscordCommand, IDiscordBotCommand
{
    public string Command => DiscordBotCommands.Start;

    public async Task Handle(IMessage message)
    {
        var channelConfig = await channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
        var messageBuilder = new StringBuilder();

        if (channelConfig.IsBotStopped)
        {
            if (!IsUserAdmin(message))
            {
                messageBuilder.AppendLine("Only admins can use this command.");
            }
            else
            {
                channelConfig.IsBotStopped = false;
                await channelConfigService.Update(channelConfig);
                messageBuilder.AppendLine("Bot started");
            }
        }
        else
        {
            messageBuilder.AppendLine("Bot is already started");
        }

        await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Start);
    }
}
