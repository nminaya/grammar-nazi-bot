using GrammarNazi.Core.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace GrammarNazi.Tests.Utilities
{
    public class TelegramUpdateHandlerTests
    {
        [Fact]
        public async Task HandleUpdate_NonSupportedUpdateTypeReceived_Should_DoNothing()
        {
            // Arrange
            var telegramBotMock = new Mock<ITelegramBotClient>();
            var loggerMock = new Mock<ILogger<TelegramUpdateHandler>>();

            var update = new Update
            {
                ChatMember = new ChatMemberUpdated()
            };

            var handler = new TelegramUpdateHandler(null, null, loggerMock.Object);

            // Act
            await handler.HandleUpdate(telegramBotMock.Object, update, default);

            // Assert

            // Verify LogInformation was called
            loggerMock.Verify(x => x.Log(
                            LogLevel.Information,
                            It.IsAny<EventId>(),
                            It.IsAny<It.IsAnyType>(),
                            It.IsAny<Exception>(),
                            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));
        }
    }
}