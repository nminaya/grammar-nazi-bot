using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord
{
    public class StopCommandTests
    {
        [Fact]
        public async Task Stop_UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new StopCommand(chatConfigurationServiceMock.Object);
            const string replyMessage = "Bot stopped";

            var chatConfig = new DiscordChannelConfig
            {
                IsBotStopped = false
            };

            var channelMock = new Mock<IMessageChannel>();
            var user = new Mock<IGuildUser>();
            user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
            var message = new Mock<IMessage>();

            message.Setup(v => v.Author).Returns(user.Object);
            message.Setup(v => v.Channel).Returns(channelMock.Object);

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message.Object);

            // Assert

            // Verify SendMessageAsync was called with the reply message "Bot stopped"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
            Assert.True(chatConfig.IsBotStopped);
        }
    }
}