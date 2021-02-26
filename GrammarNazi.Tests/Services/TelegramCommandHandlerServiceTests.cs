using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace GrammarNazi.Tests.Services
{
    public class TelegramCommandHandlerServiceTests
    {
        [Theory]
        [InlineData(TelegramBotCommands.Start)]
        [InlineData(TelegramBotCommands.Start + "@" + Defaults.TelegramBotUser)]
        public async Task Start_BotNotStopped_Should_ReplyBotStartedMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Bot is already started";

            var chatConfig = new ChatConfiguration
            {
                IsBotStopped = false
            };

            var message = new Message
            {
                Text = command,
                Chat = new Chat
                {
                    Id = 1
                }
            };

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert

            // Using It.IsAny<ChatId>() due to an issue with ChatId.Equals method.
            // We should be able to especify ChatId's after this PR gets merged https://github.com/TelegramBots/Telegram.Bot/pull/940
            // and the Telegram.Bot nuget package updated.
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.Start)]
        [InlineData(TelegramBotCommands.Start + "@" + Defaults.TelegramBotUser)]
        public async Task Start_BotNotStopped_BotNotAdmin_Should_ReplyBotNotAdminMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "The bot needs admin rights";

            var chatConfig = new ChatConfiguration
            {
                IsBotStopped = false
            };

            var message = new Message
            {
                Text = command,
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default), Times.Once);
        }

        [Theory]
        [InlineData(TelegramBotCommands.Start)]
        [InlineData(TelegramBotCommands.Start + "@" + Defaults.TelegramBotUser)]
        public async Task Start_BotStoppedAndUserNotAdmin_Should_ReplyNotAdminMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Only admins can use this command";

            var chatConfig = new ChatConfiguration
            {
                IsBotStopped = true
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(message.Chat.Id, default))
                .ReturnsAsync(new ChatMember[0]);

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.Start)]
        [InlineData(TelegramBotCommands.Start + "@" + Defaults.TelegramBotUser)]
        public async Task Start_BotStoppedAndUserAdmin_Should_ChangeChatConfig_And_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Bot started";

            var chatConfig = new ChatConfiguration
            {
                IsBotStopped = true
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.False(chatConfig.IsBotStopped);
            chatConfigurationServiceMock.Verify(v => v.Update(chatConfig));
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.Stop)]
        [InlineData(TelegramBotCommands.Stop + "@" + Defaults.TelegramBotUser)]
        public async Task Stop_UserNotAdmin_Should_ReplyMessage(string command)
        {
            await TestNotAdminUser(command);
        }

        [Theory]
        [InlineData(TelegramBotCommands.Stop)]
        [InlineData(TelegramBotCommands.Stop + "@" + Defaults.TelegramBotUser)]
        public async Task Stop_UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Bot stopped";

            var chatConfig = new ChatConfiguration
            {
                IsBotStopped = false
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.True(chatConfig.IsBotStopped);
            chatConfigurationServiceMock.Verify(v => v.Update(chatConfig));
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.HideDetails)]
        [InlineData(TelegramBotCommands.HideDetails + "@" + Defaults.TelegramBotUser)]
        public async Task HideDetails_UserIsNotAdmin_Should_ReplyMessage(string command)
        {
            await TestNotAdminUser(command);
        }

        [Theory]
        [InlineData(TelegramBotCommands.HideDetails)]
        [InlineData(TelegramBotCommands.HideDetails + "@" + Defaults.TelegramBotUser)]
        public async Task HideDetails_UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Correction details hidden";

            var chatConfig = new ChatConfiguration
            {
                HideCorrectionDetails = false
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.True(chatConfig.HideCorrectionDetails);
            chatConfigurationServiceMock.Verify(v => v.Update(chatConfig));
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.ShowDetails)]
        [InlineData(TelegramBotCommands.ShowDetails + "@" + Defaults.TelegramBotUser)]
        public async Task ShowDetails_UserIsNotAdmin_Should_ReplyMessage(string command)
        {
            await TestNotAdminUser(command);
        }

        [Theory]
        [InlineData(TelegramBotCommands.ShowDetails)]
        [InlineData(TelegramBotCommands.ShowDetails + "@" + Defaults.TelegramBotUser)]
        public async Task ShowDetails_UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Show correction details";

            var chatConfig = new ChatConfiguration
            {
                HideCorrectionDetails = true
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.False(chatConfig.HideCorrectionDetails);
            chatConfigurationServiceMock.Verify(v => v.Update(chatConfig));
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.Language)]
        [InlineData(TelegramBotCommands.Language + "@" + Defaults.TelegramBotUser)]
        public async Task Language_UserIsNotAdmin_Should_ReplyMessage(string command)
        {
            await TestNotAdminUser(command);
        }

        [Theory]
        [InlineData(TelegramBotCommands.Language, "Test")]
        [InlineData(TelegramBotCommands.Language + "@" + Defaults.TelegramBotUser, "Test")]
        [InlineData(TelegramBotCommands.Language, "fjkafdk324")]
        [InlineData(TelegramBotCommands.Language + "@" + Defaults.TelegramBotUser, "flkjsdf234")]
        public async Task Language_ParameterIsNotNumber_Should_ReplyMessage(string command, string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Invalid parameter";

            var chatConfig = new ChatConfiguration
            {
                SelectedLanguage = SupportedLanguages.Auto
            };

            var message = new Message
            {
                Text = $"{command} {parameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.Language, "500")]
        [InlineData(TelegramBotCommands.Language + "@" + Defaults.TelegramBotUser, "500")]
        [InlineData(TelegramBotCommands.Language, "123456")]
        [InlineData(TelegramBotCommands.Language + "@" + Defaults.TelegramBotUser, "123456")]
        public async Task Language_InvalidParameter_Should_ReplyMessage(string command, string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Invalid parameter";

            var chatConfig = new ChatConfiguration
            {
                SelectedLanguage = SupportedLanguages.Auto
            };

            var message = new Message
            {
                Text = $"{command} {parameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.Language, SupportedLanguages.English)]
        [InlineData(TelegramBotCommands.Language + "@" + Defaults.TelegramBotUser, SupportedLanguages.English)]
        [InlineData(TelegramBotCommands.Language, SupportedLanguages.Spanish)]
        [InlineData(TelegramBotCommands.Language + "@" + Defaults.TelegramBotUser, SupportedLanguages.Spanish)]
        public async Task Language_ValidParameter_Should_ChangeChatConfig_And_ReplyMessage(string command, SupportedLanguages languageParameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Language updated";

            var chatConfig = new ChatConfiguration
            {
                SelectedLanguage = SupportedLanguages.Auto
            };

            var message = new Message
            {
                Text = $"{command} {(int)languageParameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.Equal(languageParameter, chatConfig.SelectedLanguage);
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.SetAlgorithm)]
        [InlineData(TelegramBotCommands.SetAlgorithm + "@" + Defaults.TelegramBotUser)]
        public async Task SetAlgorithm_UserIsNotAdmin_Should_ReplyMessage(string command)
        {
            await TestNotAdminUser(command);
        }

        [Theory]
        [InlineData(TelegramBotCommands.SetAlgorithm, "Test")]
        [InlineData(TelegramBotCommands.SetAlgorithm + "@" + Defaults.TelegramBotUser, "Test")]
        [InlineData(TelegramBotCommands.SetAlgorithm, "fjkafdk324")]
        [InlineData(TelegramBotCommands.SetAlgorithm + "@" + Defaults.TelegramBotUser, "flkjsdf234")]
        public async Task SetAlgorithm_ParameterIsNotNumber_Should_ReplyMessage(string command, string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Invalid parameter";

            var chatConfig = new ChatConfiguration
            {
                GrammarAlgorithm = GrammarAlgorithms.LanguageToolApi
            };

            var message = new Message
            {
                Text = $"{command} {parameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.SetAlgorithm, "500")]
        [InlineData(TelegramBotCommands.SetAlgorithm + "@" + Defaults.TelegramBotUser, "500")]
        [InlineData(TelegramBotCommands.SetAlgorithm, "123456")]
        [InlineData(TelegramBotCommands.SetAlgorithm + "@" + Defaults.TelegramBotUser, "123456")]
        public async Task SetAlgorithm_InvalidParameter_Should_ReplyMessage(string command, string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Invalid parameter";

            var chatConfig = new ChatConfiguration
            {
                GrammarAlgorithm = GrammarAlgorithms.LanguageToolApi
            };

            var message = new Message
            {
                Text = $"{command} {parameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.SetAlgorithm, GrammarAlgorithms.LanguageToolApi)]
        [InlineData(TelegramBotCommands.SetAlgorithm + "@" + Defaults.TelegramBotUser, GrammarAlgorithms.LanguageToolApi)]
        [InlineData(TelegramBotCommands.SetAlgorithm, GrammarAlgorithms.InternalAlgorithm)]
        [InlineData(TelegramBotCommands.SetAlgorithm + "@" + Defaults.TelegramBotUser, GrammarAlgorithms.InternalAlgorithm)]
        public async Task SetAlgorithm_ValidParameter_Should_ChangeChatConfig_And_ReplyMessage(string command, GrammarAlgorithms algorithmParameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Algorithm updated";

            var chatConfig = new ChatConfiguration
            {
                GrammarAlgorithm = GrammarAlgorithms.YandexSpellerApi
            };

            var message = new Message
            {
                Text = $"{command} {(int)algorithmParameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.Equal(algorithmParameter, chatConfig.GrammarAlgorithm);
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.Tolerant)]
        [InlineData(TelegramBotCommands.Tolerant + "@" + Defaults.TelegramBotUser)]
        public async Task Tolerant_UserIsNotAdmin_Should_ReplyMessage(string command)
        {
            await TestNotAdminUser(command);
        }

        [Theory]
        [InlineData(TelegramBotCommands.Tolerant)]
        [InlineData(TelegramBotCommands.Tolerant + "@" + Defaults.TelegramBotUser)]
        public async Task Tolerant_UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Tolerant ✅";

            var chatConfig = new ChatConfiguration
            {
                CorrectionStrictnessLevel = CorrectionStrictnessLevels.Intolerant
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.Equal(CorrectionStrictnessLevels.Tolerant, chatConfig.CorrectionStrictnessLevel);
            chatConfigurationServiceMock.Verify(v => v.Update(chatConfig));
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.Intolerant)]
        [InlineData(TelegramBotCommands.Intolerant + "@" + Defaults.TelegramBotUser)]
        public async Task Intolerant_UserIsNotAdmin_Should_ReplyMessage(string command)
        {
            await TestNotAdminUser(command);
        }

        [Theory]
        [InlineData(TelegramBotCommands.Intolerant)]
        [InlineData(TelegramBotCommands.Intolerant + "@" + Defaults.TelegramBotUser)]
        public async Task Intolerant_UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Intolerant ✅";

            var chatConfig = new ChatConfiguration
            {
                CorrectionStrictnessLevel = CorrectionStrictnessLevels.Tolerant
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.Equal(CorrectionStrictnessLevels.Intolerant, chatConfig.CorrectionStrictnessLevel);
            chatConfigurationServiceMock.Verify(v => v.Update(chatConfig));
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.WhiteList)]
        [InlineData(TelegramBotCommands.WhiteList + "@" + Defaults.TelegramBotUser)]
        public async Task WhiteList_NoWhiteListsConfigured_Should_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "You don't have Whitelist words configured";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = null
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.WhiteList)]
        [InlineData(TelegramBotCommands.WhiteList + "@" + Defaults.TelegramBotUser)]
        public async Task WhiteList_WhiteListsConfigured_Should_ReplyMessageWithWhiteList(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Whitelist Words";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = new() { "Word" }
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.AddWhiteList)]
        [InlineData(TelegramBotCommands.AddWhiteList + "@" + Defaults.TelegramBotUser)]
        public async Task AddWhiteList_UserIsNotAdmin_Should_ReplyMessage(string command)
        {
            await TestNotAdminUser(command);
        }

        [Theory]
        [InlineData(TelegramBotCommands.AddWhiteList)]
        [InlineData(TelegramBotCommands.AddWhiteList + "@" + Defaults.TelegramBotUser)]
        public async Task AddWhiteList_NoParameter_Should_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Parameter not received";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = null
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.AddWhiteList, "Word")]
        [InlineData(TelegramBotCommands.AddWhiteList + "@" + Defaults.TelegramBotUser, "Word")]
        public async Task AddWhiteList_WordExist_Should_ReplyMessage(string command, string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "is already on the WhiteList";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = new() { parameter }
            };

            var message = new Message
            {
                Text = $"{command} {parameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.AddWhiteList, "Word")]
        [InlineData(TelegramBotCommands.AddWhiteList + "@" + Defaults.TelegramBotUser, "Word")]
        public async Task AddWhiteList_NoWordExist_Should_ChangeChatConfig_And_ReplyMessage(string command, string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "added to the WhiteList";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = new() { "Word1" }
            };

            var message = new Message
            {
                Text = $"{command} {parameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.Equal(2, chatConfig.WhiteListWords.Count);
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.RemoveWhiteList)]
        [InlineData(TelegramBotCommands.RemoveWhiteList + "@" + Defaults.TelegramBotUser)]
        public async Task RemoveWhiteList_UserIsNotAdmin_Should_ReplyMessage(string command)
        {
            await TestNotAdminUser(command);
        }

        [Theory]
        [InlineData(TelegramBotCommands.RemoveWhiteList)]
        [InlineData(TelegramBotCommands.RemoveWhiteList + "@" + Defaults.TelegramBotUser)]
        public async Task RemoveWhiteList_NoParameter_Should_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Parameter not received";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = null
            };

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.RemoveWhiteList, "Word")]
        [InlineData(TelegramBotCommands.RemoveWhiteList + "@" + Defaults.TelegramBotUser, "Word")]
        public async Task RemoveWhiteList_NoWordExist_Should_ReplyMessage(string command, string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "is not in the WhiteList";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = new() { "Word1" }
            };

            var message = new Message
            {
                Text = $"{command} {parameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData(TelegramBotCommands.RemoveWhiteList, "Word")]
        [InlineData(TelegramBotCommands.RemoveWhiteList + "@" + Defaults.TelegramBotUser, "Word")]
        public async Task RemoveWhiteList_WordExist_Should_RemoveWordFromWhiteList_And_ReplyMessage(string command, string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "removed from the WhiteList";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = new() { parameter }
            };

            var message = new Message
            {
                Text = $"{command} {parameter}",
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCommand(message);

            // Assert
            Assert.Empty(chatConfig.WhiteListWords);
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData("SupportedLanguages.English", SupportedLanguages.English)]
        [InlineData("SupportedLanguages.Spanish", SupportedLanguages.Spanish)]
        public async Task HandleCallBackQuery_LanguageChange_Should_ChangeSelectedLanguage(string callBackQueryData, SupportedLanguages expectedLanguage)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);

            var chatConfig = new ChatConfiguration
            {
                SelectedLanguage = SupportedLanguages.Auto
            };

            var message = new Message
            {
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            var callbackQuery = new CallbackQuery { Message = message, From = message.From, Data = callBackQueryData };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCallBackQuery(callbackQuery);

            // Assert
            Assert.Equal(expectedLanguage, chatConfig.SelectedLanguage);
        }

        [Theory]
        [InlineData("GrammarAlgorithms.DatamuseApi", GrammarAlgorithms.DatamuseApi)]
        [InlineData("GrammarAlgorithms.LanguageToolApi", GrammarAlgorithms.LanguageToolApi)]
        [InlineData("GrammarAlgorithms.YandexSpellerApi", GrammarAlgorithms.YandexSpellerApi)]
        [InlineData("GrammarAlgorithms.InternalAlgorithm", GrammarAlgorithms.InternalAlgorithm)]
        public async Task HandleCallBackQuery_AlgorithmChange_Should_ChangeSelectedAlgorithm(string callBackQueryData, GrammarAlgorithms grammarAlgorithm)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);

            var chatConfig = new ChatConfiguration
            {
                GrammarAlgorithm = GrammarAlgorithms.InternalAlgorithm
            };

            var message = new Message
            {
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            var callbackQuery = new CallbackQuery { Message = message, From = message.From, Data = callBackQueryData };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCallBackQuery(callbackQuery);

            // Assert
            Assert.Equal(grammarAlgorithm, chatConfig.GrammarAlgorithm);
        }

        [Fact]
        public async Task HandleCallBackQuery_UserNotAdmin_Should_ReplyMessage()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Only admins can use this command.";

            var chatConfig = new ChatConfiguration
            {
                GrammarAlgorithm = GrammarAlgorithms.InternalAlgorithm
            };

            var message = new Message
            {
                From = new User { Id = 2, FirstName = "User" },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            var callbackQuery = new CallbackQuery { Message = message, From = message.From, Data = "" };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = 100 } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await service.HandleCallBackQuery(callbackQuery);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Markdown, false, false, 0, null, default));
        }

        [Theory]
        [InlineData("/start@botTest")]
        [InlineData("/stop@botUsername")]
        [InlineData("/settings@botUsername")]
        public async Task CommandForAnotherBot_Should_Not_DoAnything(string command)
        {
            // Arrange
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(null, telegramBotClientMock.Object);

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            // Act
            await service.HandleCommand(message);

            // Assert

            // Make sure SendTextMessageAsync method was never called
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), ParseMode.Default, false, false, 0, null, default), Times.Never);
        }

        [Theory]
        [InlineData("/test_command")]
        [InlineData("/command")]
        [InlineData("/bot_command")]
        [InlineData("/another_command")]
        public async Task UnknownCommand_Should_Not_DoAnything(string command)
        {
            // Arrange
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(null, telegramBotClientMock.Object);

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            // Act
            await service.HandleCommand(message);

            // Assert

            // Make sure SendTextMessageAsync method was never called
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), ParseMode.Default, false, false, 0, null, default), Times.Never);
        }

        private static async Task TestNotAdminUser(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Only admins can use this command.";

            var message = new Message
            {
                Text = command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(It.IsAny<ChatId>(), default))
                .ReturnsAsync(new ChatMember[0]);

            // Act
            await service.HandleCommand(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }
    }
}