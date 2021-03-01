﻿using Discord;
using Discord.WebSocket;
using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Discord
{
    public class StartCommandTests
    {
        [Fact]
        public async Task BotNotStopped_Should_ReplyBotStartedMessage()
        {
            // Arrange
            var chatConfigurationServiceMock = new Mock<IDiscordChannelConfigService>();
            var command = new StartCommand(chatConfigurationServiceMock.Object);
            const string replyMessage = "Bot is already started";

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

            chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChannelId(message.Object.Channel.Id))
                .ReturnsAsync(chatConfig);

            // Act
            await command.Handle(message.Object);

            // Assert

            // Verify SendMessageAsync was called with the reply message "Bot is already started"
            channelMock.Verify(v => v.SendMessageAsync(null, false, It.Is<Embed>(e => e.Description.Contains(replyMessage)), null, null, null));
        }
    }
}