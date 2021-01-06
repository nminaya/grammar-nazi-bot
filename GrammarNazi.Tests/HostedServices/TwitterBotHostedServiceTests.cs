using GrammarNazi.App.HostedServices;
using GrammarNazi.Domain.Entities.Configs;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.HostedServices
{
    public class TwitterBotHostedServiceTests
    {
        [Fact]
        public async Task CancelledToken_Should_DoNothing()
        {
            // Assert
            var grammarServiceMock = new Mock<IGrammarService>();
            var cancellationTokenSource = new CancellationTokenSource();
            var loggerMock = new Mock<ILogger<TwitterBotHostedService>>();
            var twitterSettingsOptionsMock = new Mock<IOptions<TwitterBotSettings>>();
            var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15 };

            twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
            grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

            var hostedService = new TwitterBotHostedService(loggerMock.Object, new[] { grammarServiceMock.Object }, null, null, twitterSettingsOptionsMock.Object, null, null);

            // Act
            cancellationTokenSource.Cancel();
            await hostedService.StartAsync(cancellationTokenSource.Token);

            // Assert
            // If any exception thrown, test pass
        }
    }
}