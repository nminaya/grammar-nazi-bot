using GrammarNazi.App.HostedServices;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace GrammarNazi.Tests.HostedServices
{
    public class TelegramBotHostedServiceTests
    {
        public async Task OnMessage_StickerMessageReceived_Should_Not_ExecuteGrammarService()
        {
            // Arrange
            var telegramBotMock = new Mock<ITelegramBotClient>();
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource();
            var hostedService = new TelegramBotHostedService(null, telegramBotMock.Object, null, null, null, null, null);

            // TODO: Create MessageEventArgs
            // We are currently not able to create a MessageEventArgs object
            // Until this issue is fixed https://github.com/TelegramBots/Telegram.Bot/issues/926
            telegramBotMock.Raise(v => v.OnMessage += null, telegramBotMock.Object, EventArgs.Empty);

            // Act
            await hostedService.StartAsync(cancellationTokenSource.Token);

            // Assert
        }
    }
}