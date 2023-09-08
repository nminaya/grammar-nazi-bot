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

public class StopCommandTests
{
    [Fact]
    public async Task UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage()
    {
        // Arrange
        var chatConfigurationServiceMock = Substitute.For<IChatConfigurationService>();
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        var command = new StopCommand(chatConfigurationServiceMock, telegramBotClientMock);
        const string replyMessage = "Bot stopped";

        var chatConfig = new ChatConfiguration
        {
            IsBotStopped = false
        };

        var message = new Message
        {
            Text = TelegramBotCommands.Stop,
            From = new User { Id = 2 },
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        telegramBotClientMock.GetChatAdministratorsAsync(message.Chat.Id, default)
            .Returns(new[] { new ChatMemberMember { User = new() { Id = message.From.Id } } });

        chatConfigurationServiceMock.GetConfigurationByChatId(message.Chat.Id)
            .Returns(chatConfig);

        // Act
        await command.Handle(message);

        // Assert
        Assert.True(chatConfig.IsBotStopped);
        await chatConfigurationServiceMock.Received().Update(chatConfig);
        await telegramBotClientMock.Received().SendTextMessageAsync(message.Chat.Id, Arg.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
    {
        var telegramBotClientMock = Substitute.For<ITelegramBotClientWrapper>();
        await TestUtilities.TestTelegramNotAdminUser(new StopCommand(null, telegramBotClientMock), telegramBotClientMock);
    }
}
