using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord
{
    public class ShowDetailsCommandTests
    {
        [Fact]
        public async Task UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage()
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new ShowDetailsCommand(channelConfigurationServiceMock.Object);
            const string replyMessage = "Show correction details ✅";

            var chatConfig = new DiscordChannelConfig
            {
                HideCorrectionDetails = true
            };

            var channelMock = new Mock<IMessageChannel>();
            var user = new Mock<IGuildUser>();
            user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
            var message = new Mock<IMessage>();

            message.Setup(v => v.Author).Returns(user.Object);
            message.Setup(v => v.Channel).Returns(channelMock.Object);

            channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message.Object);

            // Assert

            // Verify SendMessageAsync was called with the reply message "Show correction details ✅"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None));
            channelConfigurationServiceMock.Verify(v => v.Update(chatConfig));
            Assert.False(chatConfig.HideCorrectionDetails);
        }

        [Fact]
        public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
        {
            await TestUtilities.TestDiscordNotAdminUser(new ShowDetailsCommand(null));
        }
    }
}