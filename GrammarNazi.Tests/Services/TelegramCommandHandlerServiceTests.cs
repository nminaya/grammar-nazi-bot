using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
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
        [InlineData("/start")]
        [InlineData("/start@" + Defaults.TelegramBotUser)]
        public async Task Start_NotChatCongfigured_Should_CreateChatConfigAndReplyWelcomeMessage(string command)
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var service = new TelegramCommandHandlerService(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string welcomeMessage = "Hi, I'm GrammarNazi";

            var message = new Message
            {
                Text = command,
                Chat = new Chat
                {
                    Id = 1
                }
            };

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
                .ReturnsAsync((ChatConfiguration)null);

            // Act
            await service.HandleCommand(message);

            // Assert
            chatConfigurationServiceMock.Verify(v => v.AddConfiguration(It.IsAny<ChatConfiguration>()), Times.Once);
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(welcomeMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData("/start")]
        [InlineData("/start@" + Defaults.TelegramBotUser)]
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
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(It.IsAny<ChatId>(), It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, false, false, 0, null, default));
        }

        [Theory]
        [InlineData("/start")]
        [InlineData("/start@" + Defaults.TelegramBotUser)]
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
    }
}