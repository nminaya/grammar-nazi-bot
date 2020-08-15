using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace GrammarNazi.App.HostedServices
{
    public class BotHostedService : BackgroundService
    {
        private readonly ILogger<BotHostedService> _logger;
        private readonly IEnumerable<IGrammarService> _grammarServices;
        private readonly IChatConfigurationService _chatConfigurationService;
        private readonly TelegramBotClient _client;

        public BotHostedService(ILogger<BotHostedService> logger, IEnumerable<IGrammarService> grammarServices, IChatConfigurationService chatConfigurationService)
        {
            _logger = logger;
            _grammarServices = grammarServices;
            _chatConfigurationService = chatConfigurationService;
            var apiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("Empty TELEGRAM_API_KEY");

            _client = new TelegramBotClient(apiKey);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bot Hosted Service started");

            _client.StartReceiving(cancellationToken: stoppingToken);
            _client.OnMessage += OnMessageReceived;

            // Keep hosted service alive while receiving messages
            await Task.Delay(int.MaxValue, stoppingToken);
        }

        private void OnMessageReceived(object sender, MessageEventArgs messageEvent)
        {
            _logger.LogInformation($"Message reveived: {messageEvent.Message.Text}");

            if (messageEvent.Message.Type == MessageType.Text)
            {
                // Command
                if (messageEvent.Message.Text.StartsWith('/'))
                {
                    HandleCommand(messageEvent);
                    return;
                }

                var grammarService = GetConfiguredGrammarService(messageEvent.Message.Chat.Id);

                var result = grammarService.GetCorrections(messageEvent.Message.Text).GetAwaiter().GetResult();

                if (result.HasCorrections)
                {
                    var messageBuilder = new StringBuilder();

                    foreach (var item in result.Corrections)
                    {
                        // Only suggest the first possible replacement for now
                        messageBuilder.AppendLine($"*{item.PossibleReplacements.First()}");
                    }

                    // Fire and forget for now (It returns a Task, i.e it's awaitable)
                    _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString(), replyToMessageId: messageEvent.Message.MessageId);
                }
            }
        }

        private IGrammarService GetConfiguredGrammarService(long chatId)
        {
            var chatConfig = _chatConfigurationService.GetConfigurationByChatId(chatId).GetAwaiter().GetResult();

            if (chatConfig != null)
            {
                return _grammarServices.First(v => v.GrammarAlgorith == chatConfig.GrammarAlgorithm);
            }

            return _grammarServices.First(v => v.GrammarAlgorith == GrammarAlgorithms.LanguageToolApi);
        }

        private void HandleCommand(MessageEventArgs messageEvent)
        {
            var text = messageEvent.Message.Text;

            if (IsCommand(Commands.Start, text))
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Hi, I'm GrammarNazi.");
                messageBuilder.AppendLine("I'm currently working and correcting all spelling error in this chat.");
                messageBuilder.AppendLine("Type /help to get useful commands.");

                _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
            }
            else if (IsCommand(Commands.Help, text))
            {
                _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "/settings get configured settings.");
            }
            else if (IsCommand(Commands.Settings, text))
            {
                var chatConfig = _chatConfigurationService.GetConfigurationByChatId(messageEvent.Message.Chat.Id).GetAwaiter().GetResult();

                if (chatConfig != null)
                {
                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine(GetAvailableAlgorithms());
                    messageBuilder.AppendLine($"This chat has the algorithm {chatConfig.GrammarAlgorithm} configured.");

                    _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
                }
                else
                {
                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine("Type /set_algorithm <algorithm_numer> to set an algorithm");
                    messageBuilder.AppendLine(GetAvailableAlgorithms());
                    _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
                }
            }
            else if (IsCommand(Commands.SetAlgorithm, text))
            {
                var parameters = text.Split(" ");
                if (parameters.Length == 1)
                {
                    _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Parameter not received. Type /set_algorithm <algorithm_numer> to set an algorithm.");
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int algorithm);

                    if (parsedOk)
                    {
                        var chatConfig = _chatConfigurationService.GetConfigurationByChatId(messageEvent.Message.Chat.Id).GetAwaiter().GetResult();

                        if (chatConfig != null)
                        {
                            _chatConfigurationService.Delete(chatConfig).GetAwaiter().GetResult();
                        }

                        var config = new ChatConfiguration
                        {
                            ChatId = messageEvent.Message.Chat.Id,
                            GrammarAlgorithm = (GrammarAlgorithms)algorithm
                        };

                        _chatConfigurationService.AddConfiguration(config).GetAwaiter().GetResult();
                        _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Algorithm updated.");
                    }
                    else
                    {
                        _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Invalid parameter. Type /set_algorithm <algorithm_numer> to set an algorithm.");
                    }
                }
            }

            static bool IsCommand(string expected, string actual)
            {
                if(actual.Contains("@"))
                {
                    // TODO: Get bot name from config
                    return actual.StartsWith($"{expected}@grammarNz_Bot") || actual.StartsWith($"{expected}@grammarNaziTest_Bot");
                }

                return actual.StartsWith(expected);
            }

            static string GetAvailableAlgorithms()
            {
                var algorithms = Enum.GetValues(typeof(GrammarAlgorithms)).Cast<GrammarAlgorithms>();

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Algorithms available:");

                foreach (var item in algorithms)
                {
                    messageBuilder.AppendLine($"{(int)item} - {item}");
                }

                return messageBuilder.ToString();
            }
        }
    }
}