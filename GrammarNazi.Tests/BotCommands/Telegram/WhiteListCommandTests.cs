using GrammarNazi.Core.BotCommands.Telegram;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Telegram
{
    public class WhiteListCommandTests
    {
        [Fact]
        public async Task NoWhiteListsConfigured_Should_ReplyMessage()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new WhiteListCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "You don't have Whitelist words configured";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = null
            };

            var message = new Message
            {
                Text = TelegramBotCommands.WhiteList,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(message.Chat.Id, default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Markdown, default, false, false, 0, false, default, default));
        }

        [Fact]
        public async Task WhiteListsConfigured_Should_ReplyMessageWithWhiteList()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new WhiteListCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Whitelist Words";

            var chatConfig = new ChatConfiguration
            {
                WhiteListWords = new() { "Word" }
            };

            var message = new Message
            {
                Text = TelegramBotCommands.WhiteList,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(message.Chat.Id, default))
                .ReturnsAsync(new[] { new ChatMember { User = new() { Id = message.From.Id } } });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Markdown, default, false, false, 0, false, default, default));
        }
    }
}