using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram
{
    public class IntolerantCommand : BaseTelegramCommand, ITelegramBotCommand
    {
        private readonly IChatConfigurationService _chatConfigurationService;
        private readonly ITelegramBotClient _client;

        public string Command => TelegramBotCommands.Intolerant;

        public IntolerantCommand(IChatConfigurationService chatConfigurationService,
            ITelegramBotClient telegramBotClient)
            : base(telegramBotClient)
        {
            _chatConfigurationService = chatConfigurationService;
            _client = telegramBotClient;
        }

        public async Task Handle(Message message)
        {
            if (!await IsUserAdmin(message))
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                return;
            }

            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

            chatConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant;

            await _chatConfigurationService.Update(chatConfig);

            await _client.SendTextMessageAsync(message.Chat.Id, "Intolerant ✅");

            await NotifyIfBotIsNotAdmin(message);
        }
    }
}
