using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram
{
    public class SettingsCommand : BaseTelegramCommand, ITelegramBotCommand
    {
        private readonly IChatConfigurationService _chatConfigurationService;

        public string Command => TelegramBotCommands.Settings;

        public SettingsCommand(IChatConfigurationService chatConfigurationService,
            ITelegramBotClient telegramBotClient)
            : base(telegramBotClient)
        {
            _chatConfigurationService = chatConfigurationService;
        }

        public async Task Handle(Message message)
        {
            await SendTypingNotification(message);

            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Algorithms Available:");
            messageBuilder.AppendLine(GetAvailableOptions(chatConfig.GrammarAlgorithm));
            messageBuilder.AppendLine("Supported Languages:");
            messageBuilder.AppendLine(GetAvailableOptions(chatConfig.SelectedLanguage));

            var showCorrectionDetailsIcon = chatConfig.HideCorrectionDetails ? "❌" : "✅";
            messageBuilder.AppendLine($"Show correction details {showCorrectionDetailsIcon}").AppendLine();
            messageBuilder.AppendLine("Strictness level:").AppendLine($"{chatConfig.CorrectionStrictnessLevel.GetDescription()} ✅").AppendLine();

            messageBuilder.AppendLine($"Whitelist Words:").AppendLine($"Type {TelegramBotCommands.WhiteList} to see Whitelist words configured.").AppendLine();

            if (chatConfig.IsBotStopped)
                messageBuilder.AppendLine($"The bot is currently stopped. Type {TelegramBotCommands.Start} to activate the Bot.");

            await _client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
            await NotifyIfBotIsNotAdmin(message);
        }
    }
}
