using GrammarNazi.Core.BotCommands.Telegram;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using Moq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Telegram;

public class RemoveWhiteListCommandTests
{
    [Fact]
    public async Task NoParameter_Should_ReplyMessage()
    {
        // Arrange
        var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
        var telegramBotClientMock = new Mock<ITelegramBotClientWrapper>();
        var command = new RemoveWhiteListCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
        const string replyMessage = "Parameter not received";

        var chatConfig = new ChatConfiguration
        {
            WhiteListWords = null
        };

        var message = new Message
        {
            Text = TelegramBotCommands.RemoveWhiteList,
            From = new User { Id = 2 },
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(message.Chat.Id, default))
            .ReturnsAsync(new[] { new ChatMemberMember { User = new() { Id = message.From.Id } } });

        telegramBotClientMock.Setup(v => v.GetMeAsync(default))
            .ReturnsAsync(new User { Id = 123456 });

        chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default));
    }

    [Fact]
    public async Task NoWordExist_Should_ReplyMessage()
    {
        // Arrange
        var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
        var telegramBotClientMock = new Mock<ITelegramBotClientWrapper>();
        var command = new RemoveWhiteListCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
        const string replyMessage = "is not in the WhiteList";

        var chatConfig = new ChatConfiguration
        {
            WhiteListWords = new() { "Word1" }
        };

        var message = new Message
        {
            Text = $"{TelegramBotCommands.RemoveWhiteList} Word2",
            From = new User { Id = 2 },
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(message.Chat.Id, default))
            .ReturnsAsync(new[] { new ChatMemberMember { User = new() { Id = message.From.Id } } });

        chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default));
    }

    [Fact]
    public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
    {
        var telegramBotClientMock = new Mock<ITelegramBotClientWrapper>();
        await TestUtilities.TestTelegramNotAdminUser(new RemoveWhiteListCommand(null, telegramBotClientMock.Object), telegramBotClientMock);
    }

    [Theory]
    [InlineData("Word", "Word")]
    [InlineData("Word", "word")]
    [InlineData("Word", "WORD")]
    [InlineData("Word", "WoRd")]
    public async Task WordExist_Should_RemoveWordFromWhiteList_And_ReplyMessage(string existingWord, string wordToRemove)
    {
        // Arrange
        var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
        var telegramBotClientMock = new Mock<ITelegramBotClientWrapper>();
        var command = new RemoveWhiteListCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
        const string replyMessage = "removed from the WhiteList";

        var chatConfig = new ChatConfiguration
        {
            WhiteListWords = new() { existingWord }
        };

        var message = new Message
        {
            Text = $"{TelegramBotCommands.RemoveWhiteList} {wordToRemove}",
            From = new User { Id = 2 },
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        telegramBotClientMock.Setup(v => v.GetChatAdministratorsAsync(message.Chat.Id, default))
            .ReturnsAsync(new[] { new ChatMemberMember { User = new() { Id = message.From.Id } } });

        telegramBotClientMock.Setup(v => v.GetMeAsync(default))
            .ReturnsAsync(new User { Id = 123456 });

        chatConfigurationServiceMock.Setup(v => v.GetConfigurationByChatId(message.Chat.Id))
            .ReturnsAsync(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        Assert.Empty(chatConfig.WhiteListWords);
        telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default));
    }
}
