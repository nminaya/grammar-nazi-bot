using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram
{
    public class IntolerantCommand : BaseTelegramCommand, ITelegramBotCommand
    {
        private readonly IChatConfigurationService _chatConfigurationService;

        public string Command => TelegramBotCommands.Intolerant;

        public IntolerantCommand(IChatConfigurationService chatConfigurationService,
            ITelegramBotClientWrapper telegramBotClient)
            : base(telegramBotClient)
        {
            _chatConfigurationService = chatConfigurationService;
        }

        public async Task Handle(Message message)
        {
            await SendTypingNotification(message);

            if (!await IsUserAdmin(message))
            {
                await Client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                return;
            }

            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

            chatConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant;

            await _chatConfigurationService.Update(chatConfig);

            await Client.SendTextMessageAsync(message.Chat.Id, "Intolerant ✅");

            await NotifyIfBotIsNotAdmin(message);
        }
    }
}