using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Utilities;
using Moq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GrammarNazi.Tests
{
    public static class TestUtilities
    {
        public static async Task TestDiscordNotAdminUser(IDiscordBotCommand command)
        {
            // Arrange
            const string replyMessage = "Only admins can use this command.";

            var channelMock = new Mock<IMessageChannel>();
            var user = new Mock<IGuildUser>();
            user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.None);
            var message = new Mock<IMessage>();

            message.Setup(v => v.Author).Returns(user.Object);
            message.Setup(v => v.Channel).Returns(channelMock.Object);

            // Act
            await command.Handle(message.Object);

            // Assert

            // Verify SendMessageAsync was called with the reply message "Only admins can use this command"
            channelMock.Verify(v => v.SendMessageAsync(It.Is<string>(s => s.Contains(replyMessage)), false, null, null, null, It.Is<MessageReference>(m => m.MessageId.Value == message.Object.Id), null, null, null, MessageFlags.None));
        }

        public static async Task TestTelegramNotAdminUser(ITelegramBotCommand command, Mock<ITelegramBotClientWrapper> telegramBotClientMock)
        {
            // Arrange
            const string replyMessage = "Only admins can use this command.";

            var message = new Message
            {
                Text = command.Command,
                From = new User { Id = 2 },
                Chat = new Chat
                {
                    Id = 1,
                    Type = ChatType.Group
                }
            };

            telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(message.Chat.Id, default))
                .ReturnsAsync(new ChatMember[0]);

            // Act
            await command.Handle(message);

            // Assert
            telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, message.MessageId, default, default, default));
        }
    }
}