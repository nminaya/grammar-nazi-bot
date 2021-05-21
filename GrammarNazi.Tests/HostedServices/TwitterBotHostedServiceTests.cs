using GrammarNazi.App.HostedServices;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Entities.Configs;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
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
            var sentimetAnalysisMock = new Mock<ISentimentAnalysisService>();

            twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
            grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

            var hostedService = new TwitterBotHostedService(loggerMock.Object, new[] { grammarServiceMock.Object }, null, null, twitterSettingsOptionsMock.Object, null, null, sentimetAnalysisMock.Object);

            // Act
            cancellationTokenSource.Cancel();
            await hostedService.StartAsync(cancellationTokenSource.Token);

            // Assert
            // If no exception thrown, test pass
        }

        [Fact]
        public async Task NoLastTweetId_Should_UsePageSize()
        {
            // Assert
            var twitterClientMock = new Mock<ITwitterClient>();
            var twitterLogServiceMock = new Mock<ITwitterLogService>();
            var scheduleTweetServiceMock = new Mock<IScheduledTweetService>();
            var twitterSettingsOptionsMock = new Mock<IOptions<TwitterBotSettings>>();
            var grammarServiceMock = new Mock<IGrammarService>();
            var userMock = new Mock<IUser>();
            var cancellationTokenSource = new CancellationTokenSource();
            var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15, HostedServiceIntervalMilliseconds = 150 };

            twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
            twitterLogServiceMock.Setup(v => v.GetLastTweetId()).ReturnsAsync(0);
            twitterClientMock.Setup(v => v.Users.GetFollowersAsync(twitterSettings.BotUsername))
                .ReturnsAsync(new IUser[] { userMock.Object });
            twitterClientMock.Setup(v => v.Timelines.GetUserTimelineAsync(It.IsAny<GetUserTimelineParameters>()))
                .ReturnsAsync(new ITweet[0]);
            twitterClientMock.Setup(v => v.Users.GetFriendIdsAsync(twitterSettings.BotUsername))
                .ReturnsAsync(new long[0]);
            scheduleTweetServiceMock.Setup(v => v.GetPendingScheduledTweets())
                .ReturnsAsync(Enumerable.Empty<ScheduledTweet>());
            grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

            var hostedService = new TwitterBotHostedService(Mock.Of<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock.Object }, twitterLogServiceMock.Object, twitterClientMock.Object, twitterSettingsOptionsMock.Object, null, scheduleTweetServiceMock.Object, null);

            // Act
            var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            await startTask;

            // Assert
            twitterClientMock.Verify(v => v.Timelines.GetUserTimelineAsync(It.Is<GetUserTimelineParameters>(g => g.PageSize == twitterSettings.TimelineFirstLoadPageSize)));
        }

        [Fact]
        public async Task ExistingLastTweetId_Should_UseSinceId()
        {
            // Assert
            var twitterClientMock = new Mock<ITwitterClient>();
            var twitterLogServiceMock = new Mock<ITwitterLogService>();
            var scheduleTweetServiceMock = new Mock<IScheduledTweetService>();
            var twitterSettingsOptionsMock = new Mock<IOptions<TwitterBotSettings>>();
            var grammarServiceMock = new Mock<IGrammarService>();
            var userMock = new Mock<IUser>();
            var cancellationTokenSource = new CancellationTokenSource();
            var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15, HostedServiceIntervalMilliseconds = 150 };
            const long lastTweetId = 123456;

            twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
            twitterLogServiceMock.Setup(v => v.GetLastTweetId()).ReturnsAsync(lastTweetId);
            twitterClientMock.Setup(v => v.Users.GetFollowersAsync(twitterSettings.BotUsername))
                .ReturnsAsync(new IUser[] { userMock.Object });
            twitterClientMock.Setup(v => v.Timelines.GetUserTimelineAsync(It.IsAny<GetUserTimelineParameters>()))
                .ReturnsAsync(new ITweet[0]);
            twitterClientMock.Setup(v => v.Users.GetFriendIdsAsync(twitterSettings.BotUsername))
                .ReturnsAsync(new long[0]);
            scheduleTweetServiceMock.Setup(v => v.GetPendingScheduledTweets())
                .ReturnsAsync(Enumerable.Empty<ScheduledTweet>());
            grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

            var hostedService = new TwitterBotHostedService(Mock.Of<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock.Object }, twitterLogServiceMock.Object, twitterClientMock.Object, twitterSettingsOptionsMock.Object, null, scheduleTweetServiceMock.Object, null);

            // Act
            var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            await startTask;

            // Assert
            twitterClientMock.Verify(v => v.Timelines.GetUserTimelineAsync(It.Is<GetUserTimelineParameters>(g => g.SinceId == lastTweetId)));
        }

        [Fact]
        public async Task TweetsWithRtPrefix_Should_Not_AnalyzeTweet()
        {
            // Assert
            var twitterClientMock = new Mock<ITwitterClient>();
            var twitterLogServiceMock = new Mock<ITwitterLogService>();
            var scheduleTweetServiceMock = new Mock<IScheduledTweetService>();
            var twitterSettingsOptionsMock = new Mock<IOptions<TwitterBotSettings>>();
            var grammarServiceMock = new Mock<IGrammarService>();
            var tweetMock = new Mock<ITweet>();
            var cancellationTokenSource = new CancellationTokenSource();
            var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15, HostedServiceIntervalMilliseconds = 150 };
            const long lastTweetId = 123456;

            tweetMock.Setup(v => v.Text).Returns("RT @TwitterUser This is a tweet");

            twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
            twitterLogServiceMock.Setup(v => v.GetLastTweetId()).ReturnsAsync(lastTweetId);
            twitterClientMock.Setup(v => v.Users.GetFollowerIdsAsync(twitterSettings.BotUsername))
                .ReturnsAsync(new long[] { 1 });
            twitterClientMock.Setup(v => v.Timelines.GetUserTimelineAsync(It.IsAny<GetUserTimelineParameters>()))
                .ReturnsAsync(new[] { tweetMock.Object });
            twitterClientMock.Setup(v => v.Users.GetFriendIdsAsync(twitterSettings.BotUsername))
                .ReturnsAsync(new long[0]);
            scheduleTweetServiceMock.Setup(v => v.GetPendingScheduledTweets())
                .ReturnsAsync(Enumerable.Empty<ScheduledTweet>());
            grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

            var hostedService = new TwitterBotHostedService(Mock.Of<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock.Object }, twitterLogServiceMock.Object, twitterClientMock.Object, twitterSettingsOptionsMock.Object, null, scheduleTweetServiceMock.Object, null);

            // Act
            var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            await startTask;

            // Assert
            grammarServiceMock.Verify(v => v.GetCorrections(It.IsAny<string>()), Times.Never);
            twitterLogServiceMock.Verify(v => v.LogTweet(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task CorrectTweets_Should_Not_SendReply()
        {
            // Assert
            var twitterClientMock = new Mock<ITwitterClient>();
            var twitterLogServiceMock = new Mock<ITwitterLogService>();
            var scheduleTweetServiceMock = new Mock<IScheduledTweetService>();
            var twitterSettingsOptionsMock = new Mock<IOptions<TwitterBotSettings>>();
            var grammarServiceMock = new Mock<IGrammarService>();
            var tweetMock = new Mock<ITweet>();
            var userMock = new Mock<IUser>();
            var cancellationTokenSource = new CancellationTokenSource();
            var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15, HostedServiceIntervalMilliseconds = 150 };
            const long lastTweetId = 123456;

            tweetMock.Setup(v => v.Text).Returns("This is a tweet");

            twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
            twitterLogServiceMock.Setup(v => v.GetLastTweetId()).ReturnsAsync(lastTweetId);
            twitterClientMock.Setup(v => v.Users.GetFollowersAsync(twitterSettings.BotUsername))
                .ReturnsAsync(new IUser[] { userMock.Object });
            twitterClientMock.Setup(v => v.Timelines.GetUserTimelineAsync(It.IsAny<GetUserTimelineParameters>()))
                .ReturnsAsync(new[] { tweetMock.Object });
            twitterClientMock.Setup(v => v.Users.GetFriendIdsAsync(twitterSettings.BotUsername))
                .ReturnsAsync(new long[0]);
            scheduleTweetServiceMock.Setup(v => v.GetPendingScheduledTweets())
                .ReturnsAsync(Enumerable.Empty<ScheduledTweet>());
            grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

            grammarServiceMock.Setup(v => v.GetCorrections(tweetMock.Object.Text))
                .ReturnsAsync(new GrammarCheckResult(default));

            var hostedService = new TwitterBotHostedService(Mock.Of<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock.Object }, twitterLogServiceMock.Object, twitterClientMock.Object, twitterSettingsOptionsMock.Object, null, scheduleTweetServiceMock.Object, null);

            // Act
            var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            await startTask;

            // Assert
            grammarServiceMock.Verify(v => v.GetCorrections(tweetMock.Object.Text)); // Verify GetCorrections was called

            // Verify PublishTweetAsync was never called
            twitterClientMock.Verify(v => v.Tweets.PublishTweetAsync(It.IsAny<IPublishTweetParameters>()), Times.Never);
        }
    }
}