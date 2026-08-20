using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Text;

namespace GrammarNazi.Core.BotCommands.Discord;

public class WhiteListCommand(IDiscordChannelConfigService channelConfigService) : BaseDiscordCommand, IDiscordBotCommand
{
    public string Command => DiscordBotCommands.WhiteList;

    public async Task Handle(IMessage message)
    {
        var channelConfig = await channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

        if (channelConfig.WhiteListWords?.Any() != true)
        {
            await SendMessage(message, $"You don't have Whitelist words configured. Use `{DiscordBotCommands.AddWhiteList}` to add words to the WhiteList.", DiscordBotCommands.WhiteList);
            return;
        }

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("Whitelist Words:\n");

        foreach (var word in channelConfig.WhiteListWords)
        {
            messageBuilder.AppendLine($"- {word}");
        }

        await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.WhiteList);
    }
}
