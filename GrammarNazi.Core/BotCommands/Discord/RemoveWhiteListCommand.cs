using Discord;
using Discord.WebSocket;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord
{
    public class RemoveWhiteListCommand : BaseDiscordCommand, IDiscordBotCommand
    {
        private readonly IDiscordChannelConfigService _channelConfigService;

        public string Command => DiscordBotCommands.RemoveWhiteList;

        public RemoveWhiteListCommand(IDiscordChannelConfigService discordChannelConfigService)
        {
            _channelConfigService = discordChannelConfigService;
        }

        public async Task Handle(SocketUserMessage message)
        {
            var text = message.Content;

            if (!IsUserAdmin(message))
            {
                await message.Channel.SendMessageAsync("Only admins can use this command.", messageReference: new MessageReference(message.Id));
                return;
            }

            var parameters = text.Split(" ");

            if (parameters.Length == 1)
            {
                await SendMessage(message, $"Parameter not received. Type `{DiscordBotCommands.RemoveWhiteList}` <word> to remove a Whitelist word.", DiscordBotCommands.RemoveWhiteList);
                return;
            }

            var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

            var word = parameters[1].Trim();

            if (!channelConfig.WhiteListWords.Contains(word))
            {
                await SendMessage(message, $"The word '{word}' is not in the WhiteList.", DiscordBotCommands.RemoveWhiteList);
                return;
            }

            channelConfig.WhiteListWords.Remove(word);

            await _channelConfigService.Update(channelConfig);

            await SendMessage(message, $"Word '{word}' removed from the WhiteList.", DiscordBotCommands.RemoveWhiteList);
        }
    }
}