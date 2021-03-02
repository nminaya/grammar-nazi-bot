using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord
{
    public class SetAlgorithmCommandTests
    {
        [Theory]
        [InlineData("fad123")]
        [InlineData("Test")]
        public async Task ParameterIsNotNumber_Should_ReplyMessage(string parameter)
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new SetAlgorithmCommand(channelConfigurationServiceMock.Object);
            const string replyMessage = "Invalid parameter";

            var chatConfig = new DiscordChannelConfig
            {
                GrammarAlgorithm = GrammarAlgorithms.InternalAlgorithm
            };

            var channelMock = new Mock<IMessageChannel>();
            var user = new Mock<IGuildUser>();
            user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
            var message = new Mock<IMessage>();
            message.Setup(v => v.Content).Returns($"{DiscordBotCommands.SetAlgorithm} {parameter}");
            message.Setup(v => v.Author).Returns(user.Object);
            message.Setup(v => v.Channel).Returns(channelMock.Object);

            channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message.Object);

            // Assert

            // Verify SendMessageAsync was called with the reply message "Invalid parameter"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
            Assert.Equal(GrammarAlgorithms.InternalAlgorithm, chatConfig.GrammarAlgorithm);
        }

        [Theory]
        [InlineData("500")]
        [InlineData("123456")]
        public async Task InvalidParameter_Should_ReplyMessage(string parameter)
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new SetAlgorithmCommand(channelConfigurationServiceMock.Object);
            const string replyMessage = "Invalid parameter";

            var chatConfig = new DiscordChannelConfig
            {
                GrammarAlgorithm = GrammarAlgorithms.InternalAlgorithm
            };

            var channelMock = new Mock<IMessageChannel>();
            var user = new Mock<IGuildUser>();
            user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
            var message = new Mock<IMessage>();
            message.Setup(v => v.Content).Returns($"{DiscordBotCommands.SetAlgorithm} {parameter}");
            message.Setup(v => v.Author).Returns(user.Object);
            message.Setup(v => v.Channel).Returns(channelMock.Object);

            channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message.Object);

            // Assert

            // Verify SendMessageAsync was called with the reply message "Invalid parameter"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
            Assert.Equal(GrammarAlgorithms.InternalAlgorithm, chatConfig.GrammarAlgorithm); // Make sure SelectedLanguage is still Auto
        }

        [Fact]
        public async Task ParameterNotReceived_Should_ReplyMessage()
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new SetAlgorithmCommand(channelConfigurationServiceMock.Object);
            const string replyMessage = "Parameter not received";

            var chatConfig = new DiscordChannelConfig
            {
                GrammarAlgorithm = GrammarAlgorithms.InternalAlgorithm
            };

            var channelMock = new Mock<IMessageChannel>();
            var user = new Mock<IGuildUser>();
            user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
            var message = new Mock<IMessage>();
            message.Setup(v => v.Content).Returns(DiscordBotCommands.SetAlgorithm);
            message.Setup(v => v.Author).Returns(user.Object);
            message.Setup(v => v.Channel).Returns(channelMock.Object);

            channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message.Object);

            // Assert

            // Verify SendMessageAsync was called with the reply message "Parameter not received"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
            Assert.Equal(GrammarAlgorithms.InternalAlgorithm, chatConfig.GrammarAlgorithm); // Make sure SelectedLanguage is still Auto
        }

        [Theory]
        [InlineData(GrammarAlgorithms.InternalAlgorithm)]
        [InlineData(GrammarAlgorithms.DatamuseApi)]
        [InlineData(GrammarAlgorithms.LanguageToolApi)]
        public async Task ValidParameter_Should_ChangeChatConfig_And_ReplyMessage(GrammarAlgorithms algorithmParameter)
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new SetAlgorithmCommand(channelConfigurationServiceMock.Object);
            const string replyMessage = "Algorithm updated";

            var chatConfig = new DiscordChannelConfig
            {
                GrammarAlgorithm = GrammarAlgorithms.YandexSpellerApi
            };

            var channelMock = new Mock<IMessageChannel>();
            var user = new Mock<IGuildUser>();
            user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
            var message = new Mock<IMessage>();
            message.Setup(v => v.Content).Returns($"{DiscordBotCommands.SetAlgorithm} {(int)algorithmParameter}");
            message.Setup(v => v.Author).Returns(user.Object);
            message.Setup(v => v.Channel).Returns(channelMock.Object);

            channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message.Object);

            // Assert

            // Verify SendMessageAsync was called with the reply message "Algorithm updated"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
            Assert.Equal(algorithmParameter, chatConfig.GrammarAlgorithm);
        }
    }
}