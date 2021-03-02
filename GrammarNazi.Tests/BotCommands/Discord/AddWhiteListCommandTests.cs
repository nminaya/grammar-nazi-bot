using Discord;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord
{
    public class AddWhiteListCommandTests
    {
        [Fact]
        public async Task NoParameter_Should_ReplyMessage()
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new AddWhiteListCommand(channelConfigurationServiceMock.Object);
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
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
        }

        [Fact]
        public async Task WordExist_Should_ReplyMessage()
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new AddWhiteListCommand(channelConfigurationServiceMock.Object);
            const string word = "Word";
            const string replyMessage = "is already on the WhiteList";

            var chatConfig = new DiscordChannelConfig
            {
                WhiteListWords = new() { word }
            };

            var channelMock = new Mock<IMessageChannel>();
            var user = new Mock<IGuildUser>();
            user.Setup(v => v.GuildPermissions).Returns(GuildPermissions.All);
            var message = new Mock<IMessage>();
            message.Setup(v => v.Content).Returns($"{DiscordBotCommands.AddWhiteList} {word}");
            message.Setup(v => v.Author).Returns(user.Object);
            message.Setup(v => v.Channel).Returns(channelMock.Object);

            channelConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message.Object);

            // Assert

            // Verify SendMessageAsync was called with the reply message "is already on the WhiteList"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
            Assert.Single(chatConfig.WhiteListWords);
        }

        [Fact]
        public async Task NoWordExist_Should_ChangeChatConfig_And_ReplyMessage()
        {
            // Arrange
            var channelConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new AddWhiteListCommand(channelConfigurationServiceMock.Object);
            const string replyMessage = "added to the WhiteList";

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

            // Verify SendMessageAsync was called with the reply message "added to the WhiteList"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
            Assert.Equal(2, chatConfig.WhiteListWords.Count);
        }
    }
}