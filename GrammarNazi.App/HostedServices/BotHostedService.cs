using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITelegramBotClient _client;

        public BotHostedService(ILogger<BotHostedService> logger,
            ITelegramBotClient telegramBotClient,
            IEnumerable<IGrammarService> grammarServices,
            IChatConfigurationService chatConfigurationService,
            IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _grammarServices = grammarServices;
            _chatConfigurationService = chatConfigurationService;
            _webHostEnvironment = webHostEnvironment;
            _client = telegramBotClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bot Hosted Service started");

            _client.StartReceiving(cancellationToken: stoppingToken);
            _client.OnMessage += async (obj, eventArgs) => await OnMessageReceived(obj, eventArgs);

            // Keep hosted service alive while receiving messages
            await Task.Delay(int.MaxValue, stoppingToken);
        }

        private async Task OnMessageReceived(object sender, MessageEventArgs messageEvent)
        {
            _logger.LogInformation($"Message received from chat id: {messageEvent.Message.Chat.Id}");

            if (_webHostEnvironment.IsDevelopment())
                _logger.LogInformation($"Message: {messageEvent.Message.Text}");

            if (messageEvent.Message.Type == MessageType.Text)
            {
                // Command
                if (messageEvent.Message.Text.StartsWith('/'))
                {
                    await HandleCommand(messageEvent);
                    return;
                }

                var grammarService = await GetConfiguredGrammarService(messageEvent.Message.Chat.Id);

                var result = await grammarService.GetCorrections(messageEvent.Message.Text);

                if (result.HasCorrections)
                {
                    var messageBuilder = new StringBuilder();

                    foreach (var item in result.Corrections)
                    {
                        // Only suggest the first possible replacement for now
                        messageBuilder.AppendLine($"*{item.PossibleReplacements.First()}");
                    }

                    await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString(), replyToMessageId: messageEvent.Message.MessageId);
                }
            }
        }

        private async Task<IGrammarService> GetConfiguredGrammarService(long chatId)
        {
            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(chatId);

            if (chatConfig != null)
            {
                return _grammarServices.First(v => v.GrammarAlgorith == chatConfig.GrammarAlgorithm);
            }

            return _grammarServices.First(v => v.GrammarAlgorith == GrammarAlgorithms.LanguageToolApi);
        }

        private async Task HandleCommand(MessageEventArgs messageEvent)
        {
            var text = messageEvent.Message.Text;

            if (IsCommand(Commands.Start, text))
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Hi, I'm GrammarNazi.");
                messageBuilder.AppendLine("I'm currently working and correcting all spelling errors in this chat.");
                messageBuilder.AppendLine("Type /help to get useful commands.");

                await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
            }
            else if (IsCommand(Commands.Help, text))
            {
                await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "/settings get configured settings.");
            }
            else if (IsCommand(Commands.Settings, text))
            {
                var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(messageEvent.Message.Chat.Id);

                if (chatConfig != null)
                {
                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine(GetAvailableAlgorithms());
                    messageBuilder.AppendLine($"This chat has the algorithm {chatConfig.GrammarAlgorithm.GetDescription()} configured.");

                    await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
                }
                else
                {
                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine("Type /set_algorithm <algorithm_numer> to set an algorithm");
                    messageBuilder.AppendLine(GetAvailableAlgorithms());
                    await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
                }
            }
            else if (IsCommand(Commands.SetAlgorithm, text))
            {
                var parameters = text.Split(" ");
                if (parameters.Length == 1)
                {
                    await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Parameter not received. Type /set_algorithm <algorithm_numer> to set an algorithm.");
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int algorithm);

                    if (parsedOk)
                    {
                        var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(messageEvent.Message.Chat.Id);

                        if (chatConfig != null)
                        {
                            await _chatConfigurationService.Delete(chatConfig);
                        }

                        var config = new ChatConfiguration
                        {
                            ChatId = messageEvent.Message.Chat.Id,
                            GrammarAlgorithm = (GrammarAlgorithms)algorithm
                        };

                        await _chatConfigurationService.AddConfiguration(config);
                        await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Algorithm updated.");
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Invalid parameter. Type /set_algorithm <algorithm_numer> to set an algorithm.");
                    }
                }
            }

            bool IsCommand(string expected, string actual)
            {
                if (actual.Contains("@"))
                {
                    // TODO: Get bot name from config
                    return _webHostEnvironment.IsDevelopment()
                        ? actual.StartsWith($"{expected}@grammarNaziTest_Bot")
                        : actual.StartsWith($"{expected}@grammarNz_Bot");
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
                    messageBuilder.AppendLine($"{(int)item} - {item.GetDescription()}");
                }

                return messageBuilder.ToString();
            }
        }
    }
}