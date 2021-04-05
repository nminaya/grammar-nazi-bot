using Discord;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord
{
    public class LanguageCommand : BaseDiscordCommand, IDiscordBotCommand
    {
        private readonly IDiscordChannelConfigService _channelConfigService;

        public string Command => DiscordBotCommands.Language;

        public LanguageCommand(IDiscordChannelConfigService discordChannelConfigService)
        {
            _channelConfigService = discordChannelConfigService;
        }

        public async Task Handle(IMessage message)
        {
            var messageBuilder = new StringBuilder();

            if (!IsUserAdmin(message))
            {
                messageBuilder.AppendLine("Only admins can use this command.");
                await message.Channel.SendMessageAsync(messageBuilder.ToString(), messageReference: new MessageReference(message.Id));
                return;
            }

            var parameters = message.Content.Split(" ");

            if (parameters.Length == 1)
            {
                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                messageBuilder.AppendLine($"Parameter not received. Type `{DiscordBotCommands.Language}` <language_number> to set a language.").AppendLine();
                messageBuilder.AppendLine("Languages:");
                messageBuilder.AppendLine(GetAvailableOptions(channelConfig.SelectedLanguage));
                await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Language);
                return;
            }

            bool parsedOk = int.TryParse(parameters[1], out int language);

            if (parsedOk && language.IsAssignableToEnum<SupportedLanguages>())
            {
                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
                channelConfig.SelectedLanguage = (SupportedLanguages)language;

                await _channelConfigService.Update(channelConfig);

                await SendMessage(message, $"Language updated: {channelConfig.SelectedLanguage.GetDescription()}", DiscordBotCommands.Language);
                return;
            }

            await SendMessage(message, $"Invalid parameter. Type `{DiscordBotCommands.Language}` <language_number> to set a language.", DiscordBotCommands.Language);
        }
    }
}