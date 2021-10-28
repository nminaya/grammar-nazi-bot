using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord
{
    public class WhiteListCommandTests
    {
        [Fact]
        public async Task NoWhiteListsConfigured_Should_ReplyMessage()
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new WhiteListCommand(channelConfigurationServiceMock.Object);
            const string replyMessage = "You don't have Whitelist words configured";

            var chatConfig = new DiscordChannelConfig
            {
                WhiteListWords = null
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

            // Verify SendMessageAsync was called with the reply message "You don't have Whitelist words configured"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
        }

        [Fact]
        public async Task WhiteListsConfigured_Should_ReplyMessageWithWhiteList()
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new WhiteListCommand(channelConfigurationServiceMock.Object);
            const string replyMessage = "Whitelist Words";

            var chatConfig = new DiscordChannelConfig
            {
                WhiteListWords = new() { "Word" }
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

            // Verify SendMessageAsync was called with the reply message "Whitelist Words"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
        }
    }
}