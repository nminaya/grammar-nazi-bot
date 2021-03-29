using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram
{
    public class WhiteListCommand : BaseTelegramCommand, ITelegramBotCommand
    {
        private readonly IChatConfigurationService _chatConfigurationService;

        public string Command => TelegramBotCommands.WhiteList;

        public WhiteListCommand(IChatConfigurationService chatConfigurationService,
            ITelegramBotClient telegramBotClient)
            : base(telegramBotClient)
        {
            _chatConfigurationService = chatConfigurationService;
        }

        public async Task Handle(Message message)
        {
            await SendTypingNotification(message);

            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

            if (chatConfig.WhiteListWords?.Any() == true)
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Whitelist Words:\n");

                foreach (var word in chatConfig.WhiteListWords)
                {
                    messageBuilder.AppendLine($"- {word}");
                }

                await Client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());

                return;
            }

            await Client.SendTextMessageAsync(message.Chat.Id, $"You don't have Whitelist words configured. Use {TelegramBotCommands.AddWhiteList} to add words to the WhiteList.");

            await NotifyIfBotIsNotAdmin(message);
        }
    }
}
