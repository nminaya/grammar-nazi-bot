using Discord;
using Discord.WebSocket;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord
{
    public class SetAlgorithmCommand : BaseDiscordCommand, IDiscordBotCommand
    {
        private readonly IDiscordChannelConfigService _channelConfigService;

        public string Command => DiscordBotCommands.SetAlgorithm;

        public SetAlgorithmCommand(IDiscordChannelConfigService discordChannelConfigService)
        {
            _channelConfigService = discordChannelConfigService;
        }

        public async Task Handle(SocketUserMessage message)
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

                messageBuilder.AppendLine($"Parameter not received. Type `{DiscordBotCommands.SetAlgorithm}` <algorithm_numer> to set an algorithm").AppendLine();
                messageBuilder.AppendLine(GetAvailableAlgorithms(channelConfig.GrammarAlgorithm));
                await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.SetAlgorithm);
                return;
            }

            bool parsedOk = int.TryParse(parameters[1], out int algorithm);

            if (parsedOk && algorithm.IsAssignableToEnum<GrammarAlgorithms>())
            {
                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
                channelConfig.GrammarAlgorithm = (GrammarAlgorithms)algorithm;

                await _channelConfigService.Update(channelConfig);

                await SendMessage(message, "Algorithm updated.", DiscordBotCommands.SetAlgorithm);
                return;
            }

            await SendMessage(message, $"Invalid parameter. Type `{DiscordBotCommands.SetAlgorithm}` <algorithm_numer> to set an algorithm.", DiscordBotCommands.SetAlgorithm);
        }
    }
}