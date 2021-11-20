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
    public class StartCommandTests
    {
        [Fact]
        public async Task BotNotStopped_Should_ReplyBotStartedMessage()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new StartCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Bot is already started";

            var chatConfig = new ChatConfiguration
            {
                IsBotStopped = false
            };

            var message = new Message
            {
                Text = TelegramBotCommands.Start,
                Chat = new Chat
                {
                    Id = 1
                }
            };

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Markdown, default, false, false, 0, false, default, default));
        }

        [Fact]
        public async Task BotNotStopped_BotNotAdmin_Should_ReplyBotNotAdminMessage()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new StartCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "The bot needs admin rights";

            var chatConfig = new ChatConfiguration
            {
                IsBotStopped = false
            };

            var message = new Message
            {
                Text = TelegramBotCommands.Start,
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Markdown, default, false, false, 0, false, default, default), Times.Once);
        }

        [Fact]
        public async Task BotStoppedAndUserNotAdmin_Should_ReplyNotAdminMessage()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new StartCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Only admins can use this command";

            var chatConfig = new ChatConfiguration
            {
                IsBotStopped = true
            };

            var message = new Message
            {
                Text = TelegramBotCommands.Start,
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
            await command.Handle(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Markdown, default, false, false, 0, false, default, default));
        }

        [Fact]
        public async Task BotStoppedAndUserAdmin_Should_ChangeChatConfig_And_ReplyMessage()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new StartCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Bot started";

            var chatConfig = new ChatConfiguration
            {
                IsBotStopped = true
            };

            var message = new Message
            {
                Text = TelegramBotCommands.Start,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(message.Chat.Id, default))
                .ReturnsAsync(new[] { new ChatMemberMember { User = new() { Id = message.From.Id } } });

            telegramBotClientMock.Setup(v => v.GetMeAsync(default))
                .ReturnsAsync(new User { Id = 123456 });

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message);

            // Assert
            Assert.False(chatConfig.IsBotStopped);
            chatConfigurationServiceMock.Verify(v => v.Update(chatConfig));
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Markdown, default, false, false, 0, false, default, default));
        }
    }
}