using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord;

public class RemoveWhiteListCommandTests
{
    [Fact]
    public async Task NoParameter_Should_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
        var command = new RemoveWhiteListCommand(channelConfigurationServiceMock.Object);
        const string replyMessage = "Parameter not received";

        var chatConfig = new DiscordChannelConfig
        {
            WhiteListWords = new() { "Word" }
        };

        var channelMock = new Mock<IMessageChannel>();
        var user = new Mock<IGuildUser>();
        user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
        var message = new Mock<IMessage>();
        message.Setup(v => v.Content).Returns(DiscordBotCommands.AddWhiteList);
        message.Setup(v => v.Author).Returns(user.Object);
        message.Setup(v => v.Channel).Returns(channelMock.Object);

        channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message.Object);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Parameter not received"
        channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None));
    }

    [Fact]
    public async Task NoWordExist_Should_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
        var command = new RemoveWhiteListCommand(channelConfigurationServiceMock.Object);
        const string replyMessage = "is not in the WhiteList";

        var chatConfig = new DiscordChannelConfig
        {
            WhiteListWords = new() { "Word" }
        };

        var channelMock = new Mock<IMessageChannel>();
        var user = new Mock<IGuildUser>();
        user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
        var message = new Mock<IMessage>();
        message.Setup(v => v.Content).Returns($"{DiscordBotCommands.AddWhiteList} Word2");
        message.Setup(v => v.Author).Returns(user.Object);
        message.Setup(v => v.Channel).Returns(channelMock.Object);

        channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message.Object);

        // Assert

        // Verify SendMessageAsync was called with the reply message "is not in the WhiteList"
        channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None));
    }

    [Fact]
    public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
    {
        await TestUtilities.TestDiscordNotAdminUser(new RemoveWhiteListCommand(null));
    }

    [Theory]
    [InlineData("Word", "Word")]
    [InlineData("Word", "word")]
    [InlineData("Word", "WORD")]
    [InlineData("Word", "WoRd")]
    public async Task WordExist_Should_RemoveWordFromWhiteList_And_ReplyMessage(string existingWord, string wordToRemove)
    {
        // Arrange
        var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
        var command = new RemoveWhiteListCommand(channelConfigurationServiceMock.Object);
        const string replyMessage = "removed from the WhiteList";

        var chatConfig = new DiscordChannelConfig
        {
            WhiteListWords = new() { existingWord }
        };

        var channelMock = new Mock<IMessageChannel>();
        var user = new Mock<IGuildUser>();
        user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
        var message = new Mock<IMessage>();
        message.Setup(v => v.Content).Returns($"{DiscordBotCommands.RemoveWhiteList} {wordToRemove}");
        message.Setup(v => v.Author).Returns(user.Object);
        message.Setup(v => v.Channel).Returns(channelMock.Object);

        channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message.Object);

        // Assert

        // Verify SendMessageAsync was called with the reply message "added to the WhiteList"
        channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None));
        Assert.Empty(chatConfig.WhiteListWords);
    }
}
