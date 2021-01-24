using Discord;
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

        public DiscordCommandHandlerService(IDiscordChannelConfigService channelConfigService)
        {
            _channelConfigService = channelConfigService;
        }

        public async Task HandleCommand(SocketUserMessage message)
        {
            var text = message.Content;

            if (text.StartsWith(DiscordBotCommands.Start))
            {
                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
                var messageBuilder = new StringBuilder();

                if (channelConfig == null)
                {
                    messageBuilder.AppendLine("Hi, I'm GrammarNazi.");
                    messageBuilder.AppendLine("I'm currently working and correcting all spelling errors in this channel.");
                    messageBuilder.AppendLine($"Type `{DiscordBotCommands.Help}` to get useful commands.");

                    var chatConfiguration = new DiscordChannelConfig
                    {
                        ChannelId = message.Channel.Id,
                        GrammarAlgorithm = Defaults.DefaultAlgorithm,
                        CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant,
                        SelectedLanguage = SupportedLanguages.Auto
                    };

                    await _channelConfigService.AddConfiguration(chatConfiguration);
                }
                else if (channelConfig.IsBotStopped)
                {
                    if (!IsUserAdmin(message))
                    {
                        messageBuilder.AppendLine("Only admins can use this command.");
                    }
                    else
                    {
                        channelConfig.IsBotStopped = false;
                        await _channelConfigService.Update(channelConfig);
                        messageBuilder.AppendLine("Bot started");
                    }
                }
                else
                {
                    messageBuilder.AppendLine("Bot is already started");
                }

                await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Start);
            }
            else if (text.StartsWith(DiscordBotCommands.Help))
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Useful commands:");
                messageBuilder.AppendLine($"`{DiscordBotCommands.Start}` start/activate the Bot.");
                messageBuilder.AppendLine($"`{DiscordBotCommands.Stop}` stop/disable the Bot.");
                messageBuilder.AppendLine($"`{DiscordBotCommands.Settings}` get configured settings.");
                messageBuilder.AppendLine($"`{DiscordBotCommands.SetAlgorithm}` <algorithm_number> to set an algorithm.");
                messageBuilder.AppendLine($"`{DiscordBotCommands.Language}` <language_number> to set a language.");
                messageBuilder.AppendLine($"`{DiscordBotCommands.ShowDetails}` Show correction details");
                messageBuilder.AppendLine($"`{DiscordBotCommands.HideDetails}` Hide correction details");
                messageBuilder.AppendLine($"`{DiscordBotCommands.WhiteList}` See list of ignored words.");
                messageBuilder.AppendLine($"`{DiscordBotCommands.AddWhiteList}` <word> to add a Whitelist word.");
                messageBuilder.AppendLine($"`{DiscordBotCommands.RemoveWhiteList}` <word> to remove a Whitelist word.");
                messageBuilder.AppendLine($"`{DiscordBotCommands.Tolerant}` Set strictness level to {CorrectionStrictnessLevels.Tolerant.GetDescription()}");
                messageBuilder.AppendLine($"`{DiscordBotCommands.Intolerant}` Set strictness level to {CorrectionStrictnessLevels.Intolerant.GetDescription()}");

                await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Help);
            }
            else if (text.StartsWith(DiscordBotCommands.Settings))
            {
                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(GetAvailableAlgorithms(channelConfig.GrammarAlgorithm));
                messageBuilder.AppendLine(GetSupportedLanguages(channelConfig.SelectedLanguage));

                var showCorrectionDetailsIcon = channelConfig.HideCorrectionDetails ? "❌" : "✅";
                messageBuilder.AppendLine($"Show correction details {showCorrectionDetailsIcon}").AppendLine();
                messageBuilder.AppendLine("Strictness level:").AppendLine($"{channelConfig.CorrectionStrictnessLevel.GetDescription()} ✅").AppendLine();

                messageBuilder.AppendLine($"Whitelist Words:").AppendLine($"Type `{DiscordBotCommands.WhiteList}` to see Whitelist words configured.").AppendLine();

                if (channelConfig.IsBotStopped)
                    messageBuilder.AppendLine($"The bot is currently stopped. Type `{DiscordBotCommands.Start}` to activate the Bot.");

                await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Settings);
            }
            else if (text.StartsWith(DiscordBotCommands.SetAlgorithm))
            {
                var messageBuilder = new StringBuilder();

                if (!IsUserAdmin(message))
                {
                    messageBuilder.AppendLine("Only admins can use this command.");
                    //TODO: Send reply
                    await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.SetAlgorithm);
                    return;
                }

                var parameters = text.Split(" ");
                if (parameters.Length == 1)
                {
                    var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                    messageBuilder.AppendLine($"Parameter not received. Type `{DiscordBotCommands.SetAlgorithm}` <algorithm_numer> to set an algorithm").AppendLine();
                    messageBuilder.AppendLine(GetAvailableAlgorithms(channelConfig.GrammarAlgorithm));
                    await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.SetAlgorithm);
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int algorithm);

                    if (parsedOk && algorithm.IsAssignableToEnum<GrammarAlgorithms>())
                    {
                        var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
                        channelConfig.GrammarAlgorithm = (GrammarAlgorithms)algorithm;

                        await _channelConfigService.Update(channelConfig);

                        await SendMessage(message, "Algorithm updated.", DiscordBotCommands.SetAlgorithm);
                    }
                    else
                    {
                        await SendMessage(message, $"Invalid parameter. Type `{DiscordBotCommands.SetAlgorithm}` <algorithm_numer> to set an algorithm.", DiscordBotCommands.SetAlgorithm);
                    }
                }
            }
            else if (text.StartsWith(DiscordBotCommands.Language))
            {
                var messageBuilder = new StringBuilder();

                if (!IsUserAdmin(message))
                {
                    messageBuilder.AppendLine("Only admins can use this command.");
                    //TODO: Send reply
                    await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Language);
                    return;
                }

                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                    messageBuilder.AppendLine($"Parameter not received. Type `{DiscordBotCommands.Language}` <language_number> to set a language.").AppendLine();
                    messageBuilder.AppendLine(GetSupportedLanguages(channelConfig.SelectedLanguage));
                    await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.Language);
                }
                else
                {
                    bool parsedOk = int.TryParse(parameters[1], out int language);

                    if (parsedOk && language.IsAssignableToEnum<SupportedLanguages>())
                    {
                        var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);
                        channelConfig.SelectedLanguage = (SupportedLanguages)language;

                        await _channelConfigService.Update(channelConfig);

                        await SendMessage(message, "Language updated.", DiscordBotCommands.Language);
                    }
                    else
                    {
                        await SendMessage(message, $"Invalid parameter. Type `{DiscordBotCommands.Language}` <language_number> to set a language.", DiscordBotCommands.Language);
                    }
                }
            }
            else if (text.StartsWith(DiscordBotCommands.Stop))
            {
                if (!IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.", DiscordBotCommands.Stop);
                    return;
                }

                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                channelConfig.IsBotStopped = true;

                await _channelConfigService.Update(channelConfig);

                await SendMessage(message, "Bot stopped", DiscordBotCommands.Stop);
            }
            else if (text.StartsWith(DiscordBotCommands.HideDetails))
            {
                if (!IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.", DiscordBotCommands.HideDetails);
                    return;
                }

                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                channelConfig.HideCorrectionDetails = true;

                await _channelConfigService.Update(channelConfig);

                await SendMessage(message, "Correction details hidden ✅", DiscordBotCommands.HideDetails);
            }
            else if (text.StartsWith(DiscordBotCommands.ShowDetails))
            {
                if (!IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.", DiscordBotCommands.ShowDetails);
                    return;
                }

                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                channelConfig.HideCorrectionDetails = false;

                await _channelConfigService.Update(channelConfig);

                await SendMessage(message, "Show correction details ✅", DiscordBotCommands.ShowDetails);
            }
            else if (text.StartsWith(DiscordBotCommands.Tolerant))
            {
                if (!IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.", DiscordBotCommands.Tolerant);
                    return;
                }

                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                channelConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Tolerant;

                await _channelConfigService.Update(channelConfig);

                await SendMessage(message, "Tolerant ✅", DiscordBotCommands.Tolerant);
            }
            else if (text.StartsWith(DiscordBotCommands.Intolerant))
            {
                if (!IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.", DiscordBotCommands.Intolerant);
                    return;
                }

                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                channelConfig.CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant;

                await _channelConfigService.Update(channelConfig);

                await SendMessage(message, "Intolerant ✅", DiscordBotCommands.Intolerant);
            }
            else if (text.StartsWith(DiscordBotCommands.WhiteList))
            {
                var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                if (channelConfig.WhiteListWords?.Any() == true)
                {
                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine("Whitelist Words:\n");

                    foreach (var word in channelConfig.WhiteListWords)
                    {
                        messageBuilder.AppendLine($"- {word}");
                    }

                    await SendMessage(message, messageBuilder.ToString(), DiscordBotCommands.WhiteList);

                    return;
                }

                await SendMessage(message, $"You don't have Whitelist words configured. Use `{DiscordBotCommands.AddWhiteList}` to add words to the WhiteList.", DiscordBotCommands.WhiteList);
            }
            else if (text.StartsWith(DiscordBotCommands.AddWhiteList))
            {
                if (!IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.", DiscordBotCommands.AddWhiteList);
                    return;
                }

                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    await SendMessage(message, $"Parameter not received. Type `{DiscordBotCommands.AddWhiteList}` <word> to add a Whitelist word.", DiscordBotCommands.AddWhiteList);
                }
                else
                {
                    var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                    var word = parameters[1].Trim();

                    if (channelConfig.WhiteListWords.Contains(word))
                    {
                        await SendMessage(message, $"The word '{word}' is already on the WhiteList", DiscordBotCommands.AddWhiteList);
                        return;
                    }

                    channelConfig.WhiteListWords.Add(word);

                    await _channelConfigService.Update(channelConfig);

                    await SendMessage(message, $"Word '{word}' added to the WhiteList.", DiscordBotCommands.AddWhiteList);
                }
            }
            else if (text.StartsWith(DiscordBotCommands.RemoveWhiteList))
            {
                if (!IsUserAdmin(message))
                {
                    //TODO: Send reply
                    await SendMessage(message, "Only admins can use this command.", DiscordBotCommands.RemoveWhiteList);
                    return;
                }

                var parameters = text.Split(" ");

                if (parameters.Length == 1)
                {
                    await SendMessage(message, $"Parameter not received. Type `{DiscordBotCommands.RemoveWhiteList}` <word> to remove a Whitelist word.", DiscordBotCommands.RemoveWhiteList);
                }
                else
                {
                    var channelConfig = await _channelConfigService.GetConfigurationByChannelId(message.Channel.Id);

                    var word = parameters[1].Trim();

                    if (!channelConfig.WhiteListWords.Contains(word))
                    {
                        await SendMessage(message, $"The word '{word}' is not in the WhiteList.", DiscordBotCommands.RemoveWhiteList);
                        return;
                    }

                    channelConfig.WhiteListWords.Remove(word);

                    await _channelConfigService.Update(channelConfig);

                    await SendMessage(message, $"Word '{word}' removed from the WhiteList.", DiscordBotCommands.RemoveWhiteList);
                }
            }
        }

        private static bool IsUserAdmin(SocketUserMessage message)
        {
            if (message.Channel is IPrivateChannel)
                return true;

            var user = message.Author as SocketGuildUser;
            return user.GuildPermissions.Administrator;
        }

        private static async Task SendMessage(SocketUserMessage socketUserMessage, string message, string command)
        {
            var embed = new EmbedBuilder
            {
                Color = new Color(255, 100, 0),
                Title = command,
                Description = message
            };

            await socketUserMessage.Channel.SendMessageAsync(embed: embed.Build());
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