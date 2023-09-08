using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord;

public class IntolerantCommandTests
{
    [Fact]
    public async Task UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new IntolerantCommand(channelConfigurationServiceMock);
        const string replyMessage = "Intolerant ✅";

        var chatConfig = new DiscordChannelConfig
        {
            CorrectionStrictnessLevel = CorrectionStrictnessLevels.Tolerant
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

        // Verify SendMessageAsync was called with the reply message "Intolerant ✅"
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
        await channelConfigurationServiceMock.Received().Update(chatConfig);
        Assert.Equal(CorrectionStrictnessLevels.Intolerant, chatConfig.CorrectionStrictnessLevel);
    }

    [Fact]
    public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
    {
        await TestUtilities.TestDiscordNotAdminUser(new IntolerantCommand(null));
    }
}
