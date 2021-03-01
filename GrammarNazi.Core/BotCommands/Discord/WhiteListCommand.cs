﻿using Discord.WebSocket;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord
{
    public class WhiteListCommand : BaseDiscordCommand, IDiscordBotCommand
    {
        private readonly IDiscordChannelConfigService _channelConfigService;

        public string Command => DiscordBotCommands.WhiteList;

        public WhiteListCommand(IDiscordChannelConfigService discordChannelConfigService)
        {
            _channelConfigService = discordChannelConfigService;
        }

        public async Task Handle(SocketUserMessage message)
        {
            var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

            if (channelConfig.WhiteListWords?.Any() == true)
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Whitelist Words:\n");

                foreach (var word in channelConfig.WhiteListWords)
                {
                    messageBuilder.AppendLine($"- {word}");
                }

                await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.WhiteList);

                return;
            }

            await SendMessage(message, $"You don't have Whitelist words configured. Use `{DiscordBotCommands.AddWhiteList}` to add words to the WhiteList.", DiscordBotCommands.WhiteList);
        }
    }
}