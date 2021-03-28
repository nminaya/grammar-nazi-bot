using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GrammarNazi.Core.BotCommands.Telegram
{
    public class AddWhiteListCommand : BaseTelegramCommand, ITelegramBotCommand
    {
        private readonly IChatConfigurationService _chatConfigurationService;

        public string Command => TelegramBotCommands.AddWhiteList;

        public AddWhiteListCommand(IChatConfigurationService chatConfigurationService,
            ITelegramBotClient telegramBotClient)
            : base(telegramBotClient)
        {
            _chatConfigurationService = chatConfigurationService;
        }

        public async Task Handle(Message message) 
        {
            await SendTypingNotification(message);

            if (!await IsUserAdmin(message))
            {
                await _client.SendTextMessageAsync(message.Chat.Id, "Only admins can use this command.", replyToMessageId: message.MessageId);
                return;
            }

            var parameters = message.Text.Split(" ");

            if (parameters.Length == 1)
            {
                await _client.SendTextMessageAsync(message.Chat.Id, $"Parameter not received. Type {TelegramBotCommands.AddWhiteList} <word> to add a Whitelist word.");
            }
            else
            {
                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

                var word = parameters[1].Trim();

                if (chatConfig.WhiteListWords.Contains(word))
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, $"The word '{word}' is already on the WhiteList");
                    return;
                }

                chatConfig.WhiteListWords.Add(word);

                await _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(message.Chat.Id, $"Word '{word}' added to the WhiteList.");
            }

            await NotifyIfBotIsNotAdmin(message);
        }
    }
}
