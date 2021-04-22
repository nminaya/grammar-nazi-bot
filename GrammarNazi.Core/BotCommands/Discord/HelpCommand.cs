using Discord;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.BotCommands.Discord
{
    public class HelpCommand : BaseDiscordCommand, IDiscordBotCommand
    {
        public string Command => DiscordBotCommands.Help;

        public async Task Handle(IMessage message)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Useful commands:");
            messageBuilder.AppendLine($"`{DiscordBotCommands.Start}` start/activate the Bot.");
            messageBuilder.AppendLine($"`{DiscordBotCommands.Stop}` stop/disable the Bot.");
            messageBuilder.AppendLine($"`{DiscordBotCommands.Settings}` get configured settings.");
            messageBuilder.AppendLine($"`{DiscordBotCommands.SetAlgorithm}` <algorithm_number> to set an algorithm.");
            messageBuilder.AppendLine($"`{DiscordBotCommands.Language}` <language_number> to set a language.");
            messageBuilder.AppendLine($"`{DiscordBotCommands.ShowDetails}` Show correction details");
            messageBuilder.AppendLine($"`{DiscordBotCommands.HideDetails}` Hide correction details");
            messageBuilder.AppendLine($"`{DiscordBotCommands.WhiteList}` See list of ignored words.");
            messageBuilder.AppendLine($"`{DiscordBotCommands.AddWhiteList}` <word> to add a Whitelist word.");
            messageBuilder.AppendLine($"`{DiscordBotCommands.RemoveWhiteList}` <word> to remove a Whitelist word.");
            messageBuilder.AppendLine($"`{DiscordBotCommands.Tolerant}` Set strictness level to {CorrectionStrictnessLevels.Tolerant.GetDescription()}");
            messageBuilder.AppendLine($"`{DiscordBotCommands.Intolerant}` Set strictness level to {CorrectionStrictnessLevels.Intolerant.GetDescription()}");

            await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Help);
        }
    }
}