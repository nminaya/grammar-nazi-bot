using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static GrammarNazi.Core.Utilities.TelegramBotHelper;

namespace GrammarNazi.Core.Services
{
    public class TelegramCommandHandlerService : ITelegramCommandHandlerService
    {
        private readonly IChatConfigurationService _chatConfigurationService;
        private readonly ITelegramBotClient _client;
        private readonly IEnumerable<ITelegramBotCommand> _botCommands;

        public TelegramCommandHandlerService(IChatConfigurationService chatConfigurationService,
            ITelegramBotClient telegramBotClient,
            IEnumerable<ITelegramBotCommand> botCommands)
        {
            _chatConfigurationService = chatConfigurationService;
            _client = telegramBotClient;
            _botCommands = botCommands;
        }

        public async Task HandleCommand(Message message)
        {
            var command = _botCommands.FirstOrDefault(v => IsCommand(v.Command, message.Text));

            if (command != null)
            {
                await command.Handle(message);
            }
        }

        public async Task HandleCallBackQuery(CallbackQuery callbackQuery)
        {
            var message = callbackQuery.Message;

            if (!await IsUserAdmin(_client, callbackQuery.Message, callbackQuery.From))
            {
                var userMention = $"[{callbackQuery.From.FirstName} {callbackQuery.From.LastName}](tg://user?id={callbackQuery.From.Id})";

                await _client.SendTextMessageAsync(message.Chat.Id, $"{userMention} Only admins can use this command.", ParseMode.Markdown);
                return;
            }

            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(message.Chat.Id);

            var enumTypeString = callbackQuery.Data.Split(".")[0];

            if (enumTypeString == nameof(SupportedLanguages))
            {
                var languageSelectedString = callbackQuery.Data.Split(".")[1];

                var languageSelected = Enum.GetValues(typeof(SupportedLanguages)).Cast<SupportedLanguages>().First(v => v.ToString() == languageSelectedString);

                chatConfig.SelectedLanguage = languageSelected;

                await _client.SendTextMessageAsync(message.Chat.Id, $"Language updated: {languageSelected.GetDescription()}");
            }
            else
            {
                var algorithmSelectedString = callbackQuery.Data.Split(".")[1];

                var algorithmSelected = Enum.GetValues(typeof(GrammarAlgorithms)).Cast<GrammarAlgorithms>().First(v => v.ToString() == algorithmSelectedString);

                chatConfig.GrammarAlgorithm = algorithmSelected;

                await _client.SendTextMessageAsync(message.Chat.Id, $"Algorithm updated: {algorithmSelected.GetDescription()}");
            }

            await _chatConfigurationService.Update(chatConfig);

            // Fire and forget
            _ = _client.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
        }

        private static bool IsCommand(string expected, string actual)
        {
            if (actual.Contains("@"))
            {
                return actual.StartsWith($"{expected}@{Defaults.TelegramBotUser}")
                    // For test enviroment
                    || actual.StartsWith($"{expected}@grammarNaziTest_Bot");
            }

            return actual.StartsWith(expected);
        }
    }
}