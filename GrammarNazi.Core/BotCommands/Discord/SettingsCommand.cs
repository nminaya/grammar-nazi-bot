using Discord;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord
{
    public class SettingsCommand : BaseDiscordCommand, IDiscordBotCommand
    {
        private readonly IDiscordChannelConfigService _channelConfigService;

        public string Command => DiscordBotCommands.Settings;

        public SettingsCommand(IDiscordChannelConfigService discordChannelConfigService)
        {
            _channelConfigService = discordChannelConfigService;
        }

        public async Task Handle(IMessage message)
        {
            var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Algorithms:");
            messageBuilder.AppendLine(GetAvailableOptions(channelConfig.GrammarAlgorithm));
            messageBuilder.AppendLine("Languages:");
            messageBuilder.AppendLine(GetAvailableOptions(channelConfig.SelectedLanguage));

            var showCorrectionDetailsIcon = channelConfig.HideCorrectionDetails ? "❌" : "✅";
            messageBuilder.AppendLine($"Show correction details {showCorrectionDetailsIcon}").AppendLine();
            messageBuilder.AppendLine("Strictness level:").AppendLine($"{channelConfig.CorrectionStrictnessLevel.GetDescription()} ✅").AppendLine();

            messageBuilder.AppendLine($"Whitelist Words:").AppendLine($"Type `{DiscordBotCommands.WhiteList}` to see Whitelist words configured.").AppendLine();

            if (channelConfig.IsBotStopped)
                messageBuilder.AppendLine($"The bot is currently stopped. Type `{DiscordBotCommands.Start}` to activate the Bot.");

            await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Settings);
        }
    }
}