using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GrammarNazi.App.HostedServices
{
    public class DiscordBotHostedService : BackgroundService
    {
        private readonly BaseSocketClient _client;
        private readonly DiscordSettings _discordSettings;
        private readonly ILogger<DiscordBotHostedService> _logger;
        private readonly IGithubService _githubService;
        private readonly IEnumerable<IGrammarService> _grammarServices;

        public DiscordBotHostedService(BaseSocketClient baseSocketClient,
            IOptions<DiscordSettings> options,
            IGithubService githubService,
            IEnumerable<IGrammarService> grammarServices,
            ILogger<DiscordBotHostedService> logger)
        {
            _client = baseSocketClient;
            _discordSettings = options.Value;
            _logger = logger;
            _githubService = githubService;
            _grammarServices = grammarServices;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Discord Bot Hosted Service started");

            await _client.LoginAsync(TokenType.Bot, _discordSettings.Token);

            await _client.StartAsync();

            _client.MessageReceived += async (eventArgs) =>
            {
                try
                {
                    await OnMessageReceived(eventArgs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);

                    // fire and forget
                    _ = _githubService.CreateBugIssue($"Application Exception: {ex.Message}", ex);
                }
            };

            // Keep hosted service alive while receiving messages
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task OnMessageReceived(SocketMessage arg)
        {
            if (arg is not SocketUserMessage message)
                return;

            if (message.Author.IsBot)
                return;

            _logger.LogInformation($"Message received from channel id: {message.Channel.Id}");

            // TODO: Implement command handler

            // TODO: Implement DiscordChatConfiguration
            var grammarService = GetConfiguredGrammarService();

            var corretionResult = await grammarService.GetCorrections(message.Content);

            if (!corretionResult.HasCorrections)
                return;

            var context = new SocketCommandContext((DiscordSocketClient)_client, message);

            await context.Channel.TriggerTypingAsync();

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(message.Author.Mention);

            foreach (var correction in corretionResult.Corrections)
            {
                var correctionDetailMessage = !string.IsNullOrEmpty(correction.Message)
                    ? $"[{correction.Message}]"
                    : string.Empty;

                messageBuilder.AppendLine($"*{correction.PossibleReplacements.First()} {correctionDetailMessage}");
            }

            // TODO: Wait for Discord.NET v2.3.0 release
            //await context.Channel.SendMessageAsync(messageBuilder.ToString(), messageReference: new MessageReference(message.Id));

            await context.Channel.SendMessageAsync(messageBuilder.ToString());
        }

        private IGrammarService GetConfiguredGrammarService()
        {
            // TODO: Implement get configured grammar Service
            var grammarService = _grammarServices.First(v => v.GrammarAlgorith == Defaults.DefaultAlgorithm);
            grammarService.SetSelectedLanguage(SupportedLanguages.Auto);
            grammarService.SetStrictnessLevel(CorrectionStrictnessLevels.Intolerant);

            return grammarService;
        }
    }
}