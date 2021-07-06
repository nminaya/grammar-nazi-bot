using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
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

        [Fact]
        public async Task HandleUpdate_MessageReceivedNotTextType_Should_DoNothing()
        {
            // Arrange
            var telegramBotMock = new Mock<ITelegramBotClient>();
            var chatConfigServiceMock = new Mock<IChatConfigurationService>();
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            var serviceScope = new Mock<IServiceScope>();
            var serviceProvider = new Mock<IServiceProvider>();

            var update = new Update
            {
                Message = new Message
                {
                    Audio = new Audio(),
                    Chat = new Chat { Id = 1 }
                }
            };

            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScope.Object);
            serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IChatConfigurationService))).Returns(chatConfigServiceMock.Object);

            var handler = new TelegramUpdateHandler(serviceScopeFactory.Object, null, null);

            // Act
            await handler.HandleUpdate(telegramBotMock.Object, update, default);

            // Assert
            chatConfigServiceMock.Verify(x => x.GetConfigurationByChatId(update.Message.Chat.Id), Times.Never);
        }
    }
}