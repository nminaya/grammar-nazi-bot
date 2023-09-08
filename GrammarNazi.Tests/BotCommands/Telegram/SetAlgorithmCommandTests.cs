using GrammarNazi.Core.BotCommands.Telegram;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using NSubstitute;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Telegram;

public class SetAlgorithmCommandTests
{
    [Theory]
    [InlineData("Test")]
    [InlineData("fjkafdk324")]
    public async Task ParameterIsNotNumber_Should_ReplyMessage(string parameter)
    {
        // Arrange
        var chatConfigurationServiceMock = Substitute.For<IChatConfigurationService>();
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        var command = new SetAlgorithmCommand(chatConfigurationServiceMock, telegramBotClientMock);
        const string replyMessage = "Invalid parameter";

        var chatConfig = new ChatConfiguration
        {
            GrammarAlgorithm = GrammarAlgorithms.LanguageToolApi
        };

        var message = new Message
        {
            Text = $"{TelegramBotCommands.SetAlgorithm} {parameter}",
            From = new User { Id = 2 },
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        telegramBotClientMock.GetChatAdministratorsAsync(message.Chat.Id, default)
            .Returns(new[] { new ChatMemberMember { User = new() { Id = message.From.Id } } });

        telegramBotClientMock.GetMeAsync(default)
            .Returns(new User { Id = 123456 });

        chatConfigurationServiceMock.GetConfigurationByChatId(message.Chat.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        await telegramBotClientMock.Received().SendTextMessageAsync(message.Chat.Id, Arg.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default, default);
    }

    [Theory]
    [InlineData("500")]
    [InlineData("123456")]
    public async Task InvalidParameter_Should_ReplyMessage(string parameter)
    {
        // Arrange
        var chatConfigurationServiceMock = Substitute.For<IChatConfigurationService>();
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        var command = new SetAlgorithmCommand(chatConfigurationServiceMock, telegramBotClientMock);
        const string replyMessage = "Invalid parameter";

        var chatConfig = new ChatConfiguration
        {
            GrammarAlgorithm = GrammarAlgorithms.LanguageToolApi
        };

        var message = new Message
        {
            Text = $"{TelegramBotCommands.SetAlgorithm} {parameter}",
            From = new User { Id = 2 },
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        telegramBotClientMock.GetChatAdministratorsAsync(message.Chat.Id, default)
            .Returns(new[] { new ChatMemberMember { User = new() { Id = message.From.Id } } });

        telegramBotClientMock.GetMeAsync(default)
            .Returns(new User { Id = 123456 });

        chatConfigurationServiceMock.GetConfigurationByChatId(message.Chat.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        await telegramBotClientMock.Received().SendTextMessageAsync(message.Chat.Id, Arg.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
    {
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        await TestUtilities.TestTelegramNotAdminUser(new SetAlgorithmCommand(null, telegramBotClientMock), telegramBotClientMock);
    }

    [Theory]
    [InlineData(GrammarAlgorithms.LanguageToolApi)]
    [InlineData(GrammarAlgorithms.InternalAlgorithm)]
    public async Task ValidParameter_Should_ChangeChatConfig_And_ReplyMessage(GrammarAlgorithms algorithmParameter)
    {
        // Arrange
        var chatConfigurationServiceMock = Substitute.For<IChatConfigurationService>();
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        var command = new SetAlgorithmCommand(chatConfigurationServiceMock, telegramBotClientMock);
        const string replyMessage = "Algorithm updated";

        var chatConfig = new ChatConfiguration
        {
            GrammarAlgorithm = GrammarAlgorithms.YandexSpellerApi
        };

        var message = new Message
        {
            Text = $"{TelegramBotCommands.SetAlgorithm} {(int)algorithmParameter}",
            From = new User { Id = 2 },
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        telegramBotClientMock.GetChatAdministratorsAsync(message.Chat.Id, default)
            .Returns(new[] { new ChatMemberMember { User = new() { Id = message.From.Id } } });

        telegramBotClientMock.GetMeAsync(default)
            .Returns(new User { Id = 123456 });

        chatConfigurationServiceMock.GetConfigurationByChatId(message.Chat.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        Assert.Equal(algorithmParameter, chatConfig.GrammarAlgorithm);
        await telegramBotClientMock.Received().SendTextMessageAsync(message.Chat.Id, Arg.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default, default);
    }
}
