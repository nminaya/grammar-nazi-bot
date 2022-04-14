using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord;

public class StopCommandTests
{
    [Fact]
    public async Task UserNotAdmin_Should_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
        var command = new StopCommand(channelConfigurationServiceMock.Object);
        const string replyMessage = "Only admins can use this command.";

        var chatConfig = new DiscordChannelConfig
        {
            IsBotStopped = false
        };

        var channelMock = new Mock<IMessageChannel>();
        var user = new Mock<IGuildUser>();
        user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.None);
        var message = new Mock<IMessage>();

        message.Setup(v => v.Author).Returns(user.Object);
        message.Setup(v => v.Channel).Returns(channelMock.Object);

        channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message.Object);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Only admins can use this command"
        channelMock.Verify(v => v.SendMessageAsync(replyMessage, false, null, null, null, It.Is<MessageReference>(m => m.MessageId.Value == message.Object.Id), null, null, null, MessageFlags.None));
        Assert.False(chatConfig.IsBotStopped); // Make sure IsBotStopped is still false
    }

    [Fact]
    public async Task UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
        var command = new StopCommand(channelConfigurationServiceMock.Object);
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

        channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message.Object);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Bot stopped"
        channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None));
        Assert.True(chatConfig.IsBotStopped);
    }
}
