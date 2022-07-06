using GrammarNazi.Core.BotCommands.Telegram;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using Moq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace GrammarNazi.Tests.BotCommands.Telegram;

public class IntolerantCommandTests
{
    [Fact]
    public async Task UserIsAdmin_Should_ChangeChatConfig_And_ReplyMessage()
    {
        // Arrange
        var chatConfigurationServiceMock = new Mock<IChatConfigurationService>();
        var telegramBotClientMock = new Mock<ITelegramBotClientWrapper>();
        var command = new IntolerantCommand(chatConfigurationServiceMock.Object, telegramBotClientMock.Object);
        const string replyMessage = "Intolerant ✅";

        var chatConfig = new ChatConfiguration
        {
            CorrectionStrictnessLevel = CorrectionStrictnessLevels.Tolerant
        };

        var message = new Message
        {
            Text = TelegramBotCommands.Intolerant,
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

        telegramBotClientMock.Setup(v => v.GetMeAsync(default))
            .ReturnsAsync(new User { Id = 123456 });

        // Act
        await command.Handle(message);

        // Assert
        Assert.Equal(CorrectionStrictnessLevels.Intolerant, chatConfig.CorrectionStrictnessLevel);
        chatConfigurationServiceMock.Verify(v => v.Update(chatConfig));
        telegramBotClientMock.Verify(v => v.SendTextMessageAsync(message.Chat.Id, It.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, default, default, default, default));
    }

    [Fact]
    public async Task UserNotAdmin_Should_ReplyNotAdminMessage()
    {
        var telegramBotClientMock = new Mock<ITelegramBotClientWrapper>();
        await TestUtilities.TestTelegramNotAdminUser(new IntolerantCommand(null, telegramBotClientMock.Object), telegramBotClientMock);
    }
}
