using Discord;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Text;

namespace GrammarNazi.Core.BotCommands.Discord;

public class SettingsCommand(IDiscordChannelConfigService channelConfigService) : BaseDiscordCommand, IDiscordBotCommand
{
    public string Command => DiscordBotCommands.Settings;

    public async Task Handle(IMessage message)
    {
        var channelConfig = await channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("Algorithms:");
        messageBuilder.AppendLine(GetAvailableOptions(channelConfig.GrammarAlgorithm));
        messageBuilder.AppendLine("Languages:");
        messageBuilder.AppendLine(GetAvailableOptions(channelConfig.SelectedLanguage));

        var showCorrectionDetailsIcon = channelConfig.HideCorrectionDetails ? "❌" : "✅";
        messageBuilder.AppendLine($"Show correction details {showCorrectionDetailsIcon}").AppendLine();
        messageBuilder.AppendLine("Strictness level:").AppendLine($"{channelConfig.CorrectionStrictnessLevel.Description} ✅").AppendLine();

        messageBuilder.AppendLine($"Whitelist Words:").AppendLine($"Type `{DiscordBotCommands.WhiteList}` to see Whitelist words configured.").AppendLine();

        if (channelConfig.IsBotStopped)
        {
            messageBuilder.AppendLine($"The bot is currently stopped. Type `{DiscordBotCommands.Start}` to activate the Bot.");
        }

        await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Settings);
    }
}
