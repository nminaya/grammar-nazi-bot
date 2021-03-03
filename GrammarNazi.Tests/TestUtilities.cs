using Discord;
using GrammarNazi.Domain.BotCommands;
using Moq;
using System.Threading.Tasks;

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
            channelMock.Verify(v => v.SendMessageAsync(It.Is<string>(s => s.Contains(replyMessage)), false, null, null, null, It.Is<MessageReference>(m => m.MessageId.Value == message.Object.Id)));
        }
    }
}