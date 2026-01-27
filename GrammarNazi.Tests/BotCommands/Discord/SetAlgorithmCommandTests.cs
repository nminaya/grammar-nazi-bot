using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord;

public class SetAlgorithmCommandTests
{
    [Theory]
    [InlineData("fad123")]
    [InlineData("Test")]
    public async Task ParameterIsNotNumber_Should_ReplyMessage(string parameter)
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new SetAlgorithmCommand(channelConfigurationServiceMock);
        const string replyMessage = "Invalid parameter";

        var chatConfig = new DiscordChannelConfig
        {
            GrammarAlgorithm = GrammarAlgorithms.InternalAlgorithm
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Content.Returns($"{DiscordBotCommands.SetAlgorithm} {parameter}");
        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Invalid parameter"
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
        Assert.Equal(GrammarAlgorithms.InternalAlgorithm, chatConfig.GrammarAlgorithm);
    }

    [Theory]
    [InlineData("500")]
    [InlineData("123456")]
    public async Task InvalidParameter_Should_ReplyMessage(string parameter)
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new SetAlgorithmCommand(channelConfigurationServiceMock);
        const string replyMessage = "Invalid parameter";

        var chatConfig = new DiscordChannelConfig
        {
            GrammarAlgorithm = GrammarAlgorithms.InternalAlgorithm
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Content.Returns($"{DiscordBotCommands.SetAlgorithm} {parameter}");
        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Invalid parameter"
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
        Assert.Equal(GrammarAlgorithms.InternalAlgorithm, chatConfig.GrammarAlgorithm); // Make sure SelectedLanguage is still Auto
    }

    [Fact]
    public async Task ParameterNotReceived_Should_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new SetAlgorithmCommand(channelConfigurationServiceMock);
        const string replyMessage = "Parameter not received";

        var chatConfig = new DiscordChannelConfig
        {
            GrammarAlgorithm = GrammarAlgorithms.InternalAlgorithm
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Content.Returns(DiscordBotCommands.SetAlgorithm);
        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Parameter not received"
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
        Assert.Equal(GrammarAlgorithms.InternalAlgorithm, chatConfig.GrammarAlgorithm); // Make sure SelectedLanguage is still Auto
    }

    [Fact]
    public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
    {
        await TestUtilities.TestDiscordNotAdminUser(new SetAlgorithmCommand(null));
    }

    [Theory]
    [InlineData(GrammarAlgorithms.InternalAlgorithm)]
    [InlineData(GrammarAlgorithms.DatamuseApi)]
    [InlineData(GrammarAlgorithms.GroqApi)]
    public async Task ValidParameter_Should_ChangeChatConfig_And_ReplyMessage(GrammarAlgorithms algorithmParameter)
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new SetAlgorithmCommand(channelConfigurationServiceMock);
        const string replyMessage = "Algorithm updated";

        var chatConfig = new DiscordChannelConfig
        {
            GrammarAlgorithm = GrammarAlgorithms.YandexSpellerApi
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Content.Returns($"{DiscordBotCommands.SetAlgorithm} {(int)algorithmParameter}");
        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Algorithm updated"
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
        Assert.Equal(algorithmParameter, chatConfig.GrammarAlgorithm);
    }

    [Theory]
    [InlineData(GrammarAlgorithms.LanguageToolApi)]
    [InlineData(GrammarAlgorithms.Gemini)]
    public async Task DisabledAlgorithm_Should_ReplyInvalidParameter(GrammarAlgorithms disabledAlgorithm)
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new SetAlgorithmCommand(channelConfigurationServiceMock);
        const string replyMessage = "Invalid parameter";

        var chatConfig = new DiscordChannelConfig
        {
            GrammarAlgorithm = GrammarAlgorithms.InternalAlgorithm
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Content.Returns($"{DiscordBotCommands.SetAlgorithm} {(int)disabledAlgorithm}");
        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        channelConfigurationServiceMock.GetConfigurationByChannelId(message.Channel.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        await channelMock.Received().SendMessageAsync(null, false, Arg.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None);
        Assert.Equal(GrammarAlgorithms.InternalAlgorithm, chatConfig.GrammarAlgorithm); // Should not change
    }
}
