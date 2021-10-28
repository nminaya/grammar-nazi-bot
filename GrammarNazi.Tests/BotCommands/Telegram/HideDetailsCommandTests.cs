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
    public class HideDetailsCommandTests
    {
        [Fact]
        public async Task UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            var command = new HideDetailsCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
            const string replyMessage = "Correction details hidden";

            var chatConfig = new ChatConfiguration
            {
                HideCorrectionDetails = false
            };

            var message = new Message
            {
                Text = TelegramBotCommands.HideDetails,
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
            Assert.True(chatConfig.HideCorrectionDetails);
            chatConfigurationServiceMock.Verify(v => v.Update(chatConfig));
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), ParseMode.Default, default, false, false, 0, false, default, default));
        }

        [Fact]
        public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
        {
            var telegramBotClientMock = new Mock<ITelegramBotClient>();
            await TestUtilities.TestTelegramNotAdminUser(new HideDetailsCommand(null, telegramBotClientMock.Object), telegramBotClientMock);
        }
    }
}