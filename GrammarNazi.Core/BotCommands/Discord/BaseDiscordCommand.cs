using Discord;
using Discord.WebSocket;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Enums;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord
{
    public abstract class BaseDiscordCommand
    {
        protected bool IsUserAdmin(SocketUserMessage message)
        {
            if (message.Channel is IPrivateChannel)
                return true;

            var user = message.Author as SocketGuildUser;
            return user.GuildPermissions.Administrator || user.GuildPermissions.ManageChannels;
        }

        protected async Task SendMessage(SocketUserMessage socketUserMessage, string message, string command)
        {
            var embed = new EmbedBuilder
            {
                Color = new Color(194, 12, 60),
                Title = command,
                Description = message
            };

            await socketUserMessage.Channel.SendMessageAsync(embed: embed.Build());
        }

        protected string GetAvailableAlgorithms(GrammarAlgorithms selectedAlgorith)
        {
            var algorithms = Enum.GetValues(typeof(GrammarAlgorithms)).Cast<GrammarAlgorithms>();

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Algorithms available:");

            foreach (var item in algorithms)
            {
                var selected = item == selectedAlgorith ? "✅" : "";
                messageBuilder.AppendLine($"{(int)item} - {item.GetDescription()} {selected}");
            }

            return messageBuilder.ToString();
        }

        protected string GetSupportedLanguages(SupportedLanguages selectedLanguage)
        {
            var languages = Enum.GetValues(typeof(SupportedLanguages)).Cast<SupportedLanguages>();

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Supported Languages:");

            foreach (var item in languages)
            {
                var selected = item == selectedLanguage ? "✅" : "";
                messageBuilder.AppendLine($"{(int)item} - {item} {selected}");
            }

            return messageBuilder.ToString();
        }
    }
}