using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord;

public class StopCommandTests
{
    [Fact]
    public async Task UserNotAdmin_Should_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new StopCommand(channelConfigurationServiceMock);
        const string replyMessage = "Only admins can use this command.";

        var chatConfig = new DiscordChannelConfig
        {
            IsBotStopped = false
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.None);
        var message = Substitute.For<IMessage>();

        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Only admins can use this command"
        await channelMock.Received().SendMessageAsync(replyMessage, false, null, null, null, Arg.Is<MessageReference>(m => m.MessageId.Value == message.Id), null, null, null, MessageFlags.None);
        Assert.False(chatConfig.IsBotStopped); // Make sure IsBotStopped is still false
    }

    [Fact]
    public async Task UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new StopCommand(channelConfigurationServiceMock);
        const string replyMessage = "Bot stopped";

        var chatConfig = new DiscordChannelConfig
        {
            IsBotStopped = false
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();

        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Bot stopped"
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
        Assert.True(chatConfig.IsBotStopped);
    }
}
