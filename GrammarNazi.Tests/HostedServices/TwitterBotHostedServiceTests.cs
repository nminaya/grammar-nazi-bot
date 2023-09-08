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

namespace GrammarNazi.Tests.HostedServices;

public class TwitterBotHostedServiceTests
{
    [Fact]
    public async Task CancelledToken_Should_DoNothing()
    {
        // Assert
        var grammarServiceMock = Substitute.For<IGrammarService>();
        var cancellationTokenSource = new CancellationTokenSource();
        var loggerMock = Substitute.For<ILogger<TwitterBotHostedService>>();
        var twitterSettingsOptionsMock = Substitute.For<IOptions<TwitterBotSettings>>();
        var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15 };
        var sentimetAnalysisMock = Substitute.For<ISentimentAnalysisService>();

        twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
        grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

        var hostedService = new TwitterBotHostedService(loggerMock, new[] { grammarServiceMock }, null, null, twitterSettingsOptionsMock, null, null, sentimetAnalysisMock);

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
        var twitterClientMock = Substitute.For<ITwitterClient>();
        var twitterLogServiceMock = Substitute.For<ITwitterLogService>();
        var scheduleTweetServiceMock = Substitute.For<IScheduledTweetService>();
        var twitterSettingsOptionsMock = Substitute.For<IOptions<TwitterBotSettings>>();
        var grammarServiceMock = Substitute.For<IGrammarService>();
        var userMock = Substitute.For<IUser>();
        var cancellationTokenSource = new CancellationTokenSource();
        var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15, HostedServiceIntervalMilliseconds = 150 };

        twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
        twitterLogServiceMock.Setup(v => v.GetLastTweetId()).ReturnsAsync(0);
        twitterClientMock.Setup(v => v.Users.GetFollowersAsync(twitterSettings.BotUsername))
            .ReturnsAsync(new IUser[] { userMock });
        twitterClientMock.Setup(v => v.Timelines.GetUserTimelineAsync(It.IsAny<GetUserTimelineParameters>()))
            .ReturnsAsync(new ITweet[0]);
        twitterClientMock.Setup(v => v.Users.GetFriendIdsAsync(twitterSettings.BotUsername))
            .ReturnsAsync(new long[0]);
        scheduleTweetServiceMock.Setup(v => v.GetPendingScheduledTweets())
            .ReturnsAsync(Enumerable.Empty<ScheduledTweet>());
        grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

        var hostedService = new TwitterBotHostedService(Mock.Of<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock }, twitterLogServiceMock, twitterClientMock, twitterSettingsOptionsMock, null, scheduleTweetServiceMock, null);

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
        var twitterClientMock = Substitute.For<ITwitterClient>();
        var twitterLogServiceMock = Substitute.For<ITwitterLogService>();
        var scheduleTweetServiceMock = Substitute.For<IScheduledTweetService>();
        var twitterSettingsOptionsMock = Substitute.For<IOptions<TwitterBotSettings>>();
        var grammarServiceMock = Substitute.For<IGrammarService>();
        var userMock = Substitute.For<IUser>();
        var cancellationTokenSource = new CancellationTokenSource();
        var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15, HostedServiceIntervalMilliseconds = 150 };
        const long lastTweetId = 123456;

        twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
        twitterLogServiceMock.Setup(v => v.GetLastTweetId()).ReturnsAsync(lastTweetId);
        twitterClientMock.Setup(v => v.Users.GetFollowersAsync(twitterSettings.BotUsername))
            .ReturnsAsync(new IUser[] { userMock });
        twitterClientMock.Setup(v => v.Timelines.GetUserTimelineAsync(It.IsAny<GetUserTimelineParameters>()))
            .ReturnsAsync(new ITweet[0]);
        twitterClientMock.Setup(v => v.Users.GetFriendIdsAsync(twitterSettings.BotUsername))
            .ReturnsAsync(new long[0]);
        scheduleTweetServiceMock.Setup(v => v.GetPendingScheduledTweets())
            .ReturnsAsync(Enumerable.Empty<ScheduledTweet>());
        grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

        var hostedService = new TwitterBotHostedService(Mock.Of<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock }, twitterLogServiceMock, twitterClientMock, twitterSettingsOptionsMock, null, scheduleTweetServiceMock, null);

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
        var twitterClientMock = Substitute.For<ITwitterClient>();
        var twitterLogServiceMock = Substitute.For<ITwitterLogService>();
        var scheduleTweetServiceMock = Substitute.For<IScheduledTweetService>();
        var twitterSettingsOptionsMock = Substitute.For<IOptions<TwitterBotSettings>>();
        var grammarServiceMock = Substitute.For<IGrammarService>();
        var tweetMock = Substitute.For<ITweet>();
        var cancellationTokenSource = new CancellationTokenSource();
        var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15, HostedServiceIntervalMilliseconds = 150 };
        const long lastTweetId = 123456;

        tweetMock.Setup(v => v.Text).Returns("RT @TwitterUser This is a tweet");

        twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
        twitterLogServiceMock.Setup(v => v.GetLastTweetId()).ReturnsAsync(lastTweetId);
        twitterClientMock.Setup(v => v.Users.GetFollowerIdsAsync(twitterSettings.BotUsername))
            .ReturnsAsync(new long[] { 1 });
        twitterClientMock.Setup(v => v.Timelines.GetUserTimelineAsync(It.IsAny<GetUserTimelineParameters>()))
            .ReturnsAsync(new[] { tweetMock });
        twitterClientMock.Setup(v => v.Users.GetFriendIdsAsync(twitterSettings.BotUsername))
            .ReturnsAsync(new long[0]);
        scheduleTweetServiceMock.Setup(v => v.GetPendingScheduledTweets())
            .ReturnsAsync(Enumerable.Empty<ScheduledTweet>());
        grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

        var hostedService = new TwitterBotHostedService(Mock.Of<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock }, twitterLogServiceMock, twitterClientMock, twitterSettingsOptionsMock, null, scheduleTweetServiceMock, null);

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
        var twitterClientMock = Substitute.For<ITwitterClient>();
        var twitterLogServiceMock = Substitute.For<ITwitterLogService>();
        var scheduleTweetServiceMock = Substitute.For<IScheduledTweetService>();
        var twitterSettingsOptionsMock = Substitute.For<IOptions<TwitterBotSettings>>();
        var grammarServiceMock = Substitute.For<IGrammarService>();
        var tweetMock = Substitute.For<ITweet>();
        var userMock = Substitute.For<IUser>();
        var cancellationTokenSource = new CancellationTokenSource();
        var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15, HostedServiceIntervalMilliseconds = 150 };
        const long lastTweetId = 123456;

        tweetMock.Setup(v => v.Text).Returns("This is a tweet");

        twitterSettingsOptionsMock.Setup(v => v.Value).Returns(twitterSettings);
        twitterLogServiceMock.Setup(v => v.GetLastTweetId()).ReturnsAsync(lastTweetId);
        twitterClientMock.Setup(v => v.Users.GetFollowersAsync(twitterSettings.BotUsername))
            .ReturnsAsync(new IUser[] { userMock });
        twitterClientMock.Setup(v => v.Timelines.GetUserTimelineAsync(It.IsAny<GetUserTimelineParameters>()))
            .ReturnsAsync(new[] { tweetMock });
        twitterClientMock.Setup(v => v.Users.GetFriendIdsAsync(twitterSettings.BotUsername))
            .ReturnsAsync(new long[0]);
        scheduleTweetServiceMock.Setup(v => v.GetPendingScheduledTweets())
            .ReturnsAsync(Enumerable.Empty<ScheduledTweet>());
        grammarServiceMock.Setup(v => v.GrammarAlgorith).Returns(GrammarAlgorithms.LanguageToolApi);

        grammarServiceMock.Setup(v => v.GetCorrections(tweetMock.Text))
            .ReturnsAsync(new GrammarCheckResult(default));

        var hostedService = new TwitterBotHostedService(Mock.Of<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock }, twitterLogServiceMock, twitterClientMock, twitterSettingsOptionsMock, null, scheduleTweetServiceMock, null);

        // Act
        var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();
        await startTask;

        // Assert
        grammarServiceMock.Verify(v => v.GetCorrections(tweetMock.Text)); // Verify GetCorrections was called

        // Verify PublishTweetAsync was never called
        twitterClientMock.Verify(v => v.Tweets.PublishTweetAsync(It.IsAny<IPublishTweetParameters>()), Times.Never);
    }
}
