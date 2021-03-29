using GrammarNazi.Core.Services;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Moq;
using System.Collections.Generic;
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
            var botCommandsMock = new Mock<IEnumerable<ITelegramBotCommand>>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object, botCommandsMock.Object);
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
        [InlineData(TelegramBotCommands.WhiteList)]
        [InlineData(TelegramBotCommands.WhiteList + "@" + Defaults.TelegramBotUser)]
        public async Task WhiteList_NoWhiteListsConfigured_Should_ReplyMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var botCommandsMock = new Mock<IEnumerable<ITelegramBotCommand>>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object, botCommandsMock.Object);
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
            var botCommandsMock = new Mock<IEnumerable<ITelegramBotCommand>>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object, botCommandsMock.Object);
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
        [InlineData("SupportedLanguages.English", SupportedLanguages.English)]
        [InlineData("SupportedLanguages.Spanish", SupportedLanguages.Spanish)]
        public async Task HandleCallBackQuery_LanguageChange_Should_ChangeSelectedLanguage(string callBackQueryData, SupportedLanguages expectedLanguage)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var botCommandsMock = new Mock<IEnumerable<ITelegramBotCommand>>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object, botCommandsMock.Object);

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
            var botCommandsMock = new Mock<IEnumerable<ITelegramBotCommand>>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object, botCommandsMock.Object);

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
            var botCommandsMock = new Mock<IEnumerable<ITelegramBotCommand>>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object, botCommandsMock.Object);
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
            var service = new TelegramCommandHandlerService(null, telegramBotClientMock.Object, null);

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
            var service = new TelegramCommandHandlerService(null, telegramBotClientMock.Object, null);

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
            var botCommandsMock = new Mock<IEnumerable<ITelegramBotCommand>>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object, botCommandsMock.Object);
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