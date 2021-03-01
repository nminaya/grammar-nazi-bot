using Discord.WebSocket;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord
{
    public class StartCommand : BaseDiscordCommand, IDiscordBotCommand
    {
        private readonly IDiscordChannelConfigService _channelConfigService;

        public string Command => DiscordBotCommands.Start;

        public StartCommand(IDiscordChannelConfigService channelConfigService)
        {
            _channelConfigService = channelConfigService;
        }

        public async Task Handle(SocketUserMessage message)
        {
            var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
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
                    await _channelConfigService.Update(channelConfig);
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
}