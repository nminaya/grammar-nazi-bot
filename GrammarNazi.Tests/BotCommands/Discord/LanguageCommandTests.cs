using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord;

public class LanguageCommandTests
{
    [Theory]
    [InlineData("fad123")]
    [InlineData("Test")]
    public async Task ParameterIsNotNumber_Should_ReplyMessage(string parameter)
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new LanguageCommand(channelConfigurationServiceMock);
        const string replyMessage = "Invalid parameter";

        var chatConfig = new DiscordChannelConfig
        {
            SelectedLanguage = SupportedLanguages.Auto
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Setup(v => v.Content).Returns($"{DiscordBotCommands.Language} {parameter}");
        message.Setup(v => v.Author).Returns(user);
        message.Setup(v => v.Channel).Returns(channelMock);

        channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Channel.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Invalid parameter"
        channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None));
        Assert.Equal(SupportedLanguages.Auto, chatConfig.SelectedLanguage); // Make sure SelectedLanguage is still Auto
    }

    [Theory]
    [InlineData("500")]
    [InlineData("123456")]
    public async Task InvalidParameter_Should_ReplyMessage(string parameter)
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new LanguageCommand(channelConfigurationServiceMock);
        const string replyMessage = "Invalid parameter";

        var chatConfig = new DiscordChannelConfig
        {
            SelectedLanguage = SupportedLanguages.Auto
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Setup(v => v.Content).Returns($"{DiscordBotCommands.Language} {parameter}");
        message.Setup(v => v.Author).Returns(user);
        message.Setup(v => v.Channel).Returns(channelMock);

        channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Channel.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Invalid parameter"
        channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None));
        Assert.Equal(SupportedLanguages.Auto, chatConfig.SelectedLanguage); // Make sure SelectedLanguage is still Auto
    }

    [Fact]
    public async Task ParameterNotReceived_Should_ReplyMessage()
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new LanguageCommand(channelConfigurationServiceMock);
        const string replyMessage = "Parameter not received";

        var chatConfig = new DiscordChannelConfig
        {
            SelectedLanguage = SupportedLanguages.Auto
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Setup(v => v.Content).Returns(DiscordBotCommands.Language);
        message.Setup(v => v.Author).Returns(user);
        message.Setup(v => v.Channel).Returns(channelMock);

        channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Channel.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Parameter not received"
        channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None));
        Assert.Equal(SupportedLanguages.Auto, chatConfig.SelectedLanguage); // Make sure SelectedLanguage is still Auto
    }

    [Theory]
    [InlineData(SupportedLanguages.English)]
    [InlineData(SupportedLanguages.Spanish)]
    [InlineData(SupportedLanguages.French)]
    public async Task ValidParameter_Should_ChangeChatConfig_And_ReplyMessage(SupportedLanguages languageParameter)
    {
        // Arrange
        var channelConfigurationServiceMock = Substitute.For<IDiscordChannelConfigService>();
        var command = new LanguageCommand(channelConfigurationServiceMock);
        const string replyMessage = "Language updated";

        var chatConfig = new DiscordChannelConfig
        {
            SelectedLanguage = SupportedLanguages.Auto
        };

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
        var message = Substitute.For<IMessage>();
        message.Setup(v => v.Content).Returns($"{DiscordBotCommands.Language} {(int)languageParameter}");
        message.Setup(v => v.Author).Returns(user);
        message.Setup(v => v.Channel).Returns(channelMock);

        channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Channel.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Language updated"
        channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null, null, null, null, MessageFlags.None));
        Assert.Equal(languageParameter, chatConfig.SelectedLanguage);
    }
}
