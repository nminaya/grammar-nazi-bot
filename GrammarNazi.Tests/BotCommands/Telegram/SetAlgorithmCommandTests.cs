using GrammarNazi.Core.BotCommands.Telegram;
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

namespace GrammarNazi.Tests.BotCommands.Telegram
{
    public class SetAlgorithmCommandTests
    {
        [Theory]
        [InlineData("Test")]
        [InlineData("fjkafdk324")]
        public async Task ParameterIsNotNumber_Should_ReplyMessage(string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new SetAlgorithmCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Invalid parameter";

            var chatConfig = new ChatConfiguration
            {
                GrammarAlgorithm = GrammarAlgorithms.LanguageToolApi
            };

            var message = new Message
            {
                Text = $"{TelegramBotCommands.SetAlgorithm} {parameter}",
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
            await command.Handle(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, default, false, false, 0, false, default, default));
        }

        [Theory]
        [InlineData("500")]
        [InlineData("123456")]
        public async Task InvalidParameter_Should_ReplyMessage(string parameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new SetAlgorithmCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Invalid parameter";

            var chatConfig = new ChatConfiguration
            {
                GrammarAlgorithm = GrammarAlgorithms.LanguageToolApi
            };

            var message = new Message
            {
                Text = $"{TelegramBotCommands.SetAlgorithm} {parameter}",
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
            await command.Handle(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, default, false, false, 0, false, default, default));
        }

        [Fact]
        public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
        {
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            await TestUtilities.TestTelegramNotAdminUser(new SetAlgorithmCommand(null, telegramBotClientMock.Object), telegramBotClientMock);
        }

        [Theory]
        [InlineData(GrammarAlgorithms.LanguageToolApi)]
        [InlineData(GrammarAlgorithms.InternalAlgorithm)]
        public async Task ValidParameter_Should_ChangeChatConfig_And_ReplyMessage(GrammarAlgorithms algorithmParameter)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new SetAlgorithmCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Algorithm updated";

            var chatConfig = new ChatConfiguration
            {
                GrammarAlgorithm = GrammarAlgorithms.YandexSpellerApi
            };

            var message = new Message
            {
                Text = $"{TelegramBotCommands.SetAlgorithm} {(int)algorithmParameter}",
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
            await command.Handle(message);

            // Assert
            Assert.Equal(algorithmParameter, chatConfig.GrammarAlgorithm);
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, default, false, false, 0, false, default, default));
        }
    }
}
