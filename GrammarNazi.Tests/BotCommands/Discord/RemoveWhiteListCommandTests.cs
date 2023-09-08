using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord;

public class RemoveWhiteListCommandTests
{
    [Fact]
    public async Task NoParameter_Should_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new RemoveWhiteListCommand(channelConfigurationServiceMock);
        const string replyMessage = "Parameter not received";

        var chatConfig = new DiscordChannelConfig
        {
            WhiteListWords = new() { "Word" }
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Content.Returns(DiscordBotCommands.AddWhiteList);
        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Parameter not received"
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
    }

    [Fact]
    public async Task NoWordExist_Should_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new RemoveWhiteListCommand(channelConfigurationServiceMock);
        const string replyMessage = "is not in the WhiteList";

        var chatConfig = new DiscordChannelConfig
        {
            WhiteListWords = new() { "Word" }
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Content.Returns($"{DiscordBotCommands.AddWhiteList} Word2");
        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "is not in the WhiteList"
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
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
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new RemoveWhiteListCommand(channelConfigurationServiceMock);
        const string replyMessage = "removed from the WhiteList";

        var chatConfig = new DiscordChannelConfig
        {
            WhiteListWords = new() { existingWord }
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Content.Returns($"{DiscordBotCommands.RemoveWhiteList} {wordToRemove}");
        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "added to the WhiteList"
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
        Assert.Empty(chatConfig.WhiteListWords);
    }
}
