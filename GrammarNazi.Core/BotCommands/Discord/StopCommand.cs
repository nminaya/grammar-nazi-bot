using Discord;
using Discord.WebSocket;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord
{
    public class StopCommand : BaseDiscordCommand, IDiscordBotCommand
    {
        private readonly IDiscordChannelConfigService _channelConfigService;

        public string Command => DiscordBotCommands.Stop;

        public StopCommand(IDiscordChannelConfigService discordChannelConfigService)
        {
            _channelConfigService = discordChannelConfigService;
        }

        public async Task Handle(IMessage message)
        {
            if (!IsUserAdmin(message))
            {
                await message.Channel.SendMessageAsync("Only admins can use this command.", messageReference: new MessageReference(message.Id));
                return;
            }

            var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

            channelConfig.IsBotStopped = true;

            await _channelConfigService.Update(channelConfig);

            await SendMessage(message, "Bot stopped", DiscordBotCommands.Stop);
        }
    }
}