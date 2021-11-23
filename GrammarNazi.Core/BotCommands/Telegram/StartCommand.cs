using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram
{
    public class StartCommand : BaseTelegramCommand, ITelegramBotCommand
    {
        private readonly IChatConfigurationService _chatConfigurationService;

        public string Command => TelegramBotCommands.Start;

        public StartCommand(IChatConfigurationService chatConfigurationService,
            ITelegramBotClientWrapper telegramBotClient)
            : base(telegramBotClient)
        {
            _chatConfigurationService = chatConfigurationService;
        }

        public async Task Handle(Message message)
        {
            await SendTypingNotification(message);

            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);
            var messageBuilder = new StringBuilder();

            if (chatConfig.IsBotStopped)
            {
                if (!await IsUserAdmin(message))
                {
                    messageBuilder.AppendLine("Only admins can use this command.");
                }
                else
                {
                    chatConfig.IsBotStopped = false;
                    await _chatConfigurationService.Update(chatConfig);
                    messageBuilder.AppendLine("Bot started");
                }
            }
            else
            {
                messageBuilder.AppendLine("Bot is already started");
            }

            await Client.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
            await NotifyIfBotIsNotAdmin(message);
        }
    }
}