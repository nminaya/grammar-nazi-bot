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
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ILogger<TelegramBotHostedService> _logger;
        private readonly IEnumerable<IGrammarService> _grammarServices;
        private readonly IChatConfigurationService _chatConfigurationService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITelegramBotClient _client;

        public TelegramBotHostedService(ILogger<TelegramBotHostedService> logger,
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

            if (messageEvent.Message.Type != MessageType.Text) // We only analyze Text messages
                return;

            if (_webHostEnvironment.IsDevelopment())
                _logger.LogInformation($"Message: {messageEvent.Message.Text}");

            if (messageEvent.Message.Text.StartsWith('/')) // Text is a command
            {
                await HandleCommand(messageEvent);
                return;
            }

            var chatConfig = await GetChatConfiguration(messageEvent.Message.Chat.Id);

            if (chatConfig.IsBotStopped)
                return;

            var grammarService = GetConfiguredGrammarService(chatConfig);

            var corretionResult = await grammarService.GetCorrections(messageEvent.Message.Text);

            if (corretionResult.HasCorrections)
            {
                var messageBuilder = new StringBuilder();

                foreach (var correction in corretionResult.Corrections)
                {
                    // Only suggest the first possible replacement for now
                    var message = string.IsNullOrEmpty(correction.Message) ? string.Empty : $"[{correction.Message}]";
                    messageBuilder.AppendLine($"*{correction.PossibleReplacements.First()} {message}");
                }

                await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString(), replyToMessageId: messageEvent.Message.MessageId);
            }
        }

        private async Task<ChatConfiguration> GetChatConfiguration(long chatId)
        {
            var chatConfig = await _chatConfigurationService.GetConfigurationByChatId(chatId);

            if (chatConfig != null)
                return chatConfig;

            var chatConfiguration = new ChatConfiguration
            {
                ChatId = chatId,
                GrammarAlgorithm = Defaults.DefaultAlgorithm,
                SelectedLanguage = SupportedLanguages.Auto
            };

            await _chatConfigurationService.AddConfiguration(chatConfiguration);

            return chatConfiguration;
        }

        private IGrammarService GetConfiguredGrammarService(ChatConfiguration chatConfig)
        {
            var grammarService = _grammarServices.First(v => v.GrammarAlgorith == chatConfig.GrammarAlgorithm);
            grammarService.SetSelectedLanguage(chatConfig.SelectedLanguage);

            return grammarService;
        }

        private async Task HandleCommand(MessageEventArgs messageEvent)
        {
            var text = messageEvent.Message.Text;

            // TODO: Evaluate moving all this logic into a service, and do a refactor

            if (IsCommand(Commands.Start, text))
            {
                var chatConfig = await GetChatConfiguration(messageEvent.Message.Chat.Id);
                var messageBuilder = new StringBuilder();

                if (chatConfig == null)
                {
                    messageBuilder.AppendLine("Hi, I'm GrammarNazi.");
                    messageBuilder.AppendLine("I'm currently working and correcting all spelling errors in this chat.");
                    messageBuilder.AppendLine($"Type {Commands.Help} to get useful commands.");

                    var chatConfiguration = new ChatConfiguration
                    {
                        ChatId = messageEvent.Message.Chat.Id,
                        GrammarAlgorithm = Defaults.DefaultAlgorithm,
                        SelectedLanguage = SupportedLanguages.Auto
                    };

                    await _chatConfigurationService.AddConfiguration(chatConfiguration);
                }
                else
                {
                    if (chatConfig.IsBotStopped)
                    {
                        chatConfig.IsBotStopped = false;
                        await _chatConfigurationService.Update(chatConfig);
                        messageBuilder.AppendLine("Bot started");
                    }
                    else
                    {
                        messageBuilder.AppendLine("Bot is already started");
                    }
                }

                await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
            }
            else if (IsCommand(Commands.Help, text))
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Help").AppendLine();
                messageBuilder.AppendLine("Useful commands:");
                messageBuilder.AppendLine($"{Commands.Start} start/activate the Bot.");
                messageBuilder.AppendLine($"{Commands.Stop} stop/disable the Bot.");
                messageBuilder.AppendLine($"{Commands.Settings} get configured settings.");
                messageBuilder.AppendLine($"{Commands.SetAlgorithm} <algorithm_numer> to set an algorithm.");
                messageBuilder.AppendLine($"{Commands.Language} <language_number> to set a language.");
                await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
            }
            else if (IsCommand(Commands.Settings, text))
            {
                var chatConfig = await GetChatConfiguration(messageEvent.Message.Chat.Id);

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(GetAvailableAlgorithms(chatConfig.GrammarAlgorithm));
                messageBuilder.AppendLine(GetSupportedLanguages(chatConfig.SelectedLanguage));

                if (chatConfig.IsBotStopped)
                    messageBuilder.AppendLine($"The bot is currently stopped. Type {Commands.Start} the Bot.");

                await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
            }
            else if (IsCommand(Commands.SetAlgorithm, text))
            {
                var parameters = text.Split(" ");
                if (parameters.Length == 1)
                {
                    var chatConfig = await GetChatConfiguration(messageEvent.Message.Chat.Id);

                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine($"Parameter not received. Type {Commands.SetAlgorithm} <algorithm_numer> to set an algorithm").AppendLine();
                    messageBuilder.AppendLine(GetAvailableAlgorithms(chatConfig.GrammarAlgorithm));
                    await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int algorithm);

                    if (parsedOk)
                    {
                        var chatConfig = await GetChatConfiguration(messageEvent.Message.Chat.Id);
                        chatConfig.GrammarAlgorithm = (GrammarAlgorithms)algorithm;

                        await _chatConfigurationService.Update(chatConfig);

                        await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Algorithm updated.");
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, $"Invalid parameter. Type {Commands.SetAlgorithm} <algorithm_numer> to set an algorithm.");
                    }
                }
            }
            else if (IsCommand(Commands.Language, text))
            {
                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    var chatConfig = await GetChatConfiguration(messageEvent.Message.Chat.Id);

                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine($"Parameter not received. Type {Commands.Language} <language_number> to set a language.").AppendLine();
                    messageBuilder.AppendLine(GetSupportedLanguages(chatConfig.SelectedLanguage));
                    await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, messageBuilder.ToString());
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int language);

                    if (parsedOk)
                    {
                        var chatConfig = await GetChatConfiguration(messageEvent.Message.Chat.Id);
                        chatConfig.SelectedLanguage = (SupportedLanguages)language;

                        await _chatConfigurationService.Update(chatConfig);

                        await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, "Language updated.");
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, $"Invalid parameter. Type {Commands.Language} <language_number> to set a language.");
                    }
                }
            }
            else if (IsCommand(Commands.Stop, text))
            {
                var chatConfig = await GetChatConfiguration(messageEvent.Message.Chat.Id);

                chatConfig.IsBotStopped = true;

                await _chatConfigurationService.Update(chatConfig);

                await _client.SendTextMessageAsync(messageEvent.Message.Chat.Id, $"Bot stopped");
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

            static string GetAvailableAlgorithms(GrammarAlgorithms selectedAlgorith)
            {
                var algorithms = Enum.GetValues(typeof(GrammarAlgorithms)).Cast<GrammarAlgorithms>();

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Algorithms available:");

                foreach (var item in algorithms)
                {
                    var selected = item == selectedAlgorith ? "✅" : "";
                    messageBuilder.AppendLine($"{(int)item} - {item.GetDescription()} {selected}");
                }

                return messageBuilder.ToString();
            }

            static string GetSupportedLanguages(SupportedLanguages selectedLanguage)
            {
                var languages = Enum.GetValues(typeof(SupportedLanguages)).Cast<SupportedLanguages>();

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Supported Languages:");

                foreach (var item in languages)
                {
                    var selected = item == selectedLanguage ? "✅" : "";
                    messageBuilder.AppendLine($"{(int)item} - {item} {selected}");
                }

                return messageBuilder.ToString();
            }
        }
    }
}