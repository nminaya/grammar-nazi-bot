using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram
{
    public class HelpCommand : BaseTelegramCommand, ITelegramBotCommand
    {
        private readonly ITelegramBotClient _client;

        public string Command => TelegramBotCommands.Help;

        public HelpCommand(ITelegramBotClient telegramBotClient)
            : base(telegramBotClient)
        {
            _client = telegramBotClient;
        }

        public async Task Handle(Message message)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Help").AppendLine();
            messageBuilder.AppendLine("Useful commands:");
            messageBuilder.AppendLine($"{TelegramBotCommands.Start} start/activate the Bot.");
            messageBuilder.AppendLine($"{TelegramBotCommands.Stop} stop/disable the Bot.");
            messageBuilder.AppendLine($"{TelegramBotCommands.Settings} get configured settings.");
            messageBuilder.AppendLine($"{TelegramBotCommands.SetAlgorithm} <algorithm_number> to set an algorithm.");
            messageBuilder.AppendLine($"{TelegramBotCommands.Language} <language_number> to set a language.");
            messageBuilder.AppendLine($"{TelegramBotCommands.ShowDetails} Show correction details");
            messageBuilder.AppendLine($"{TelegramBotCommands.HideDetails} Hide correction details");
            messageBuilder.AppendLine($"{TelegramBotCommands.WhiteList} See list of ignored words.");
            messageBuilder.AppendLine($"{TelegramBotCommands.AddWhiteList} <word> to add a Whitelist word.");
            messageBuilder.AppendLine($"{TelegramBotCommands.RemoveWhiteList} <word> to remove a Whitelist word.");
            messageBuilder.AppendLine($"{TelegramBotCommands.Tolerant} Set strictness level to {CorrectionStrictnessLevels.Tolerant.GetDescription()}");
            messageBuilder.AppendLine($"{TelegramBotCommands.Intolerant} Set strictness level to {CorrectionStrictnessLevels.Intolerant.GetDescription()}");

            await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
            await NotifyIfBotIsNotAdmin(message);
        }
    }
}