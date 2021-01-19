using Discord.Commands;
using Discord.WebSocket;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class DiscordCommandHandlerService : IDiscordCommandHandlerService
    {
        private readonly IDiscordChannelConfigService _channelConfigService;
        private readonly BaseSocketClient _client;

        public DiscordCommandHandlerService(IDiscordChannelConfigService channelConfigService,
            BaseSocketClient client)
        {
            _channelConfigService = channelConfigService;
            _client = client;
        }

        public async Task HandleCommand(SocketUserMessage message)
        {
            var text = message.Content;

            if (text.StartsWith(DiscordBotCommands.Start))
            {
                var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
                var messageBuilder = new StringBuilder();

                if (chatConfig == null)
                {
                    messageBuilder.AppendLine("Hi, I'm GrammarNazi.");
                    messageBuilder.AppendLine("I'm currently working and correcting all spelling errors in this chat.");
                    messageBuilder.AppendLine($"Type {DiscordBotCommands.Help} to get useful commands.");

                    var chatConfiguration = new DiscordChannelConfig
                    {
                        ChannelId = message.Channel.Id,
                        GrammarAlgorithm = Defaults.DefaultAlgorithm,
                        CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant,
                        SelectedLanguage = SupportedLanguages.Auto
                    };

                    await _channelConfigService.AddConfiguration(chatConfiguration);
                }
                else if (chatConfig.IsBotStopped)
                {
                    if (!await IsUserAdmin(message))
                    {
                        messageBuilder.AppendLine("Only admins can use this command.");
                    }
                    else
                    {
                        chatConfig.IsBotStopped = false;
                        await _channelConfigService.Update(chatConfig);
                        messageBuilder.AppendLine("Bot started");
                    }
                }
                else
                {
                    messageBuilder.AppendLine("Bot is already started");
                }

                await SendMessage(message, messageBuilder.ToString());
            }
            else if (text.StartsWith(DiscordBotCommands.Help))
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Help").AppendLine();
                messageBuilder.AppendLine("Useful commands:");
                messageBuilder.AppendLine($"{DiscordBotCommands.Start} start/activate the Bot.");
                messageBuilder.AppendLine($"{DiscordBotCommands.Stop} stop/disable the Bot.");
                messageBuilder.AppendLine($"{DiscordBotCommands.Settings} get configured settings.");
                messageBuilder.AppendLine($"{DiscordBotCommands.SetAlgorithm} <algorithm_number> to set an algorithm.");
                messageBuilder.AppendLine($"{DiscordBotCommands.Language} <language_number> to set a language.");
                messageBuilder.AppendLine($"{DiscordBotCommands.ShowDetails} Show correction details");
                messageBuilder.AppendLine($"{DiscordBotCommands.HideDetails} Hide correction details");
                messageBuilder.AppendLine($"{DiscordBotCommands.WhiteList} See list of ignored words.");
                messageBuilder.AppendLine($"{DiscordBotCommands.AddWhiteList} <word> to add a Whitelist word.");
                messageBuilder.AppendLine($"{DiscordBotCommands.RemoveWhiteList} <word> to remove a Whitelist word.");
                messageBuilder.AppendLine($"{DiscordBotCommands.Tolerant} Set strictness level to {CorrectionStrictnessLevels.Tolerant.GetDescription()}");
                messageBuilder.AppendLine($"{DiscordBotCommands.Intolerant} Set strictness level to {CorrectionStrictnessLevels.Intolerant.GetDescription()}");

                await SendMessage(message, messageBuilder.ToString());
            }
            else if (text.StartsWith(DiscordBotCommands.Settings))
            {
                var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(GetAvailableAlgorithms(chatConfig.GrammarAlgorithm));
                messageBuilder.AppendLine(GetSupportedLanguages(chatConfig.SelectedLanguage));

                var showCorrectionDetailsIcon = chatConfig.HideCorrectionDetails ? "❌" : "✅";
                messageBuilder.AppendLine($"Show correction details {showCorrectionDetailsIcon}").AppendLine();
                messageBuilder.AppendLine("Strictness level:").AppendLine($"{chatConfig.CorrectionStrictnessLevel.GetDescription()} ✅").AppendLine();

                messageBuilder.AppendLine($"Whitelist Words:").AppendLine($"Type {DiscordBotCommands.WhiteList} to see Whitelist words configured.").AppendLine();

                if (chatConfig.IsBotStopped)
                    messageBuilder.AppendLine($"The bot is currently stopped. Type {DiscordBotCommands.Start} to activate the Bot.");

                await SendMessage(message, messageBuilder.ToString());
            }
            else if (text.StartsWith(DiscordBotCommands.SetAlgorithm))
            {
                var messageBuilder = new StringBuilder();

                if (!await IsUserAdmin(message))
                {
                    messageBuilder.AppendLine("Only admins can use this command.");
                    //TODO: Send reply
                    await SendMessage(message, messageBuilder.ToString());
                    return;
                }

                var parameters = text.Split(" ");
                if (parameters.Length == 1)
                {
                    var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                    messageBuilder.AppendLine($"Parameter not received. Type {DiscordBotCommands.SetAlgorithm} <algorithm_numer> to set an algorithm").AppendLine();
                    messageBuilder.AppendLine(GetAvailableAlgorithms(chatConfig.GrammarAlgorithm));
                    await SendMessage(message, messageBuilder.ToString());
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int algorithm);

                    if (parsedOk && algorithm.IsAssignableToEnum<GrammarAlgorithms>())
                    {
                        var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
                        chatConfig.GrammarAlgorithm = (GrammarAlgorithms)algorithm;

                        await _channelConfigService.Update(chatConfig);

                        await SendMessage(message, "Algorithm updated.");
                    }
                    else
                    {
                        await SendMessage(message, $"Invalid parameter. Type {DiscordBotCommands.SetAlgorithm} <algorithm_numer> to set an algorithm.");
                    }
                }
            }
            else if (text.StartsWith(DiscordBotCommands.Language))
            {
                var messageBuilder = new StringBuilder();

                if (!await IsUserAdmin(message))
                {
                    messageBuilder.AppendLine("Only admins can use this command.");
                    //TODO: Send reply
                    await SendMessage(message, messageBuilder.ToString());
                    return;
                }

                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                    messageBuilder.AppendLine($"Parameter not received. Type {DiscordBotCommands.Language} <language_number> to set a language.").AppendLine();
                    messageBuilder.AppendLine(GetSupportedLanguages(chatConfig.SelectedLanguage));
                    await SendMessage(message, messageBuilder.ToString());
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int language);

                    if (parsedOk && language.IsAssignableToEnum<SupportedLanguages>())
                    {
                        var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
                        chatConfig.SelectedLanguage = (SupportedLanguages)language;

                        await _channelConfigService.Update(chatConfig);

                        await SendMessage(message, "Language updated.");
                    }
                    else
                    {
                        await SendMessage(message, $"Invalid parameter. Type {DiscordBotCommands.Language} <language_number> to set a language.");
                    }
                }
            }
            else if (text.StartsWith(DiscordBotCommands.Stop))
            {
                if (!await IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.");
                    return;
                }

                var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                chatConfig.IsBotStopped = true;

                await _channelConfigService.Update(chatConfig);

                await SendMessage(message, "Bot stopped");
            }
            else if (text.StartsWith(DiscordBotCommands.HideDetails))
            {
                if (!await IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.");
                    return;
                }

                var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                chatConfig.HideCorrectionDetails = true;

                await _channelConfigService.Update(chatConfig);

                await SendMessage(message, "Correction details hidden ✅");
            }
            else if (text.StartsWith(DiscordBotCommands.ShowDetails))
            {
                if (!await IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.");
                    return;
                }

                var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                chatConfig.HideCorrectionDetails = false;

                await _channelConfigService.Update(chatConfig);

                await SendMessage(message, "Show correction details ✅");
            }
            else if (text.StartsWith(DiscordBotCommands.Tolerant))
            {
                if (!await IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.");
                    return;
                }

                var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                chatConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Tolerant;

                await _channelConfigService.Update(chatConfig);

                await SendMessage(message, "Tolerant ✅");
            }
            else if (text.StartsWith(DiscordBotCommands.Intolerant))
            {
                if (!await IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.");
                    return;
                }

                var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                chatConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant;

                await _channelConfigService.Update(chatConfig);

                await SendMessage(message, "Intolerant ✅");
            }
            else if (text.StartsWith(DiscordBotCommands.WhiteList))
            {
                var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                if (chatConfig.WhiteListWords?.Any() == true)
                {
                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine("Whitelist Words:\n");

                    foreach (var word in chatConfig.WhiteListWords)
                    {
                        messageBuilder.AppendLine($"- {word}");
                    }

                    await SendMessage(message, messageBuilder.ToString());

                    return;
                }

                await SendMessage(message, $"You don't have Whitelist words configured. Use {DiscordBotCommands.AddWhiteList} to add words to the WhiteList.");
            }
            else if (text.StartsWith(DiscordBotCommands.AddWhiteList))
            {
                if (!await IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.");
                    return;
                }

                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    await SendMessage(message, $"Parameter not received. Type {DiscordBotCommands.AddWhiteList} <word> to add a Whitelist word.");
                }
                else
                {
                    var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                    var word = parameters[1].Trim();

                    if (chatConfig.WhiteListWords.Contains(word))
                    {
                        await SendMessage(message, $"The word '{word}' is already on the WhiteList");
                        return;
                    }

                    chatConfig.WhiteListWords.Add(word);

                    await _channelConfigService.Update(chatConfig);

                    await SendMessage(message, $"Word '{word}' added to the WhiteList.");
                }
            }
            else if (text.StartsWith(DiscordBotCommands.RemoveWhiteList))
            {
                if (!await IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.");
                    return;
                }

                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    await SendMessage(message, $"Parameter not received. Type {DiscordBotCommands.RemoveWhiteList} <word> to remove a Whitelist word.");
                }
                else
                {
                    var chatConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                    var word = parameters[1].Trim();

                    if (!chatConfig.WhiteListWords.Contains(word))
                    {
                        await SendMessage(message, $"The word '{word}' is not in the WhiteList.");
                        return;
                    }

                    chatConfig.WhiteListWords.Remove(word);

                    await _channelConfigService.Update(chatConfig);

                    await SendMessage(message, $"Word '{word}' removed from the WhiteList.");
                }
            }
        }

        private async Task<bool> IsUserAdmin(SocketUserMessage message)
        {
            return true;
        }

        private async Task SendMessage(SocketUserMessage socketUserMessage, string message)
        {
            var context = new SocketCommandContext((DiscordSocketClient)_client, socketUserMessage);

            await context.Channel.SendMessageAsync(message);
        }

        private static string GetAvailableAlgorithms(GrammarAlgorithms selectedAlgorith)
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

        private static string GetSupportedLanguages(SupportedLanguages selectedLanguage)
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