using GrammarNazi.Core.BotCommands.Telegram;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using NSubstitute;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Telegram;

public class StartCommandTests
{
    [Fact]
    public async Task BotNotStopped_Should_ReplyBotStartedMessage()
    {
        // Arrange
        var chatConfigurationServiceMock = Substitute.For<IChatConfigurationService>();
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        var command = new StartCommand(chatConfigurationServiceMock, telegramBotClientMock);
        const string replyMessage = "Bot is already started";

        var chatConfig = new ChatConfiguration
        {
            IsBotStopped = false
        };

        var message = new Message
        {
            Text = TelegramBotCommands.Start,
            Chat = new Chat
            {
                Id = 1
            }
        };

        chatConfigurationServiceMock.GetConfigurationByChatId(message.Chat.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        await telegramBotClientMock.Received().SendTextMessageAsync(message.Chat.Id, Arg.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task BotNotStopped_BotNotAdmin_Should_ReplyBotNotAdminMessage()
    {
        // Arrange
        var chatConfigurationServiceMock = Substitute.For<IChatConfigurationService>();
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        var command = new StartCommand(chatConfigurationServiceMock, telegramBotClientMock);
        const string replyMessage = "The bot needs admin rights";

        var chatConfig = new ChatConfiguration
        {
            IsBotStopped = false
        };

        var message = new Message
        {
            Text = TelegramBotCommands.Start,
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        chatConfigurationServiceMock.GetConfigurationByChatId(message.Chat.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        await telegramBotClientMock.Received().SendTextMessageAsync(message.Chat.Id, Arg.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task BotStoppedAndUserNotAdmin_Should_ReplyNotAdminMessage()
    {
        // Arrange
        var chatConfigurationServiceMock = Substitute.For<IChatConfigurationService>();
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        var command = new StartCommand(chatConfigurationServiceMock, telegramBotClientMock);
        const string replyMessage = "Only admins can use this command";

        var chatConfig = new ChatConfiguration
        {
            IsBotStopped = true
        };

        var message = new Message
        {
            Text = TelegramBotCommands.Start,
            From = new User { Id = 2 },
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        telegramBotClientMock.GetChatAdministratorsAsync(message.Chat.Id, default)
            .Returns(new ChatMember[0]);

        chatConfigurationServiceMock.GetConfigurationByChatId(message.Chat.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        await telegramBotClientMock.Received().SendTextMessageAsync(message.Chat.Id, Arg.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task BotStoppedAndUserAdmin_Should_ChangeChatConfig_And_ReplyMessage()
    {
        // Arrange
        var chatConfigurationServiceMock = Substitute.For<IChatConfigurationService>();
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        var command = new StartCommand(chatConfigurationServiceMock, telegramBotClientMock);
        const string replyMessage = "Bot started";

        var chatConfig = new ChatConfiguration
        {
            IsBotStopped = true
        };

        var message = new Message
        {
            Text = TelegramBotCommands.Start,
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
        Assert.False(chatConfig.IsBotStopped);
        await chatConfigurationServiceMock.Received().Update(chatConfig);
        await telegramBotClientMock.Received().SendTextMessageAsync(message.Chat.Id, Arg.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default);
    }
}
