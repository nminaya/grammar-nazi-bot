using GrammarNazi.App.HostedServices;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Entities.Configs;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
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

        twitterSettingsOptionsMock.Value.Returns(twitterSettings);
        grammarServiceMock.GrammarAlgorithm.Returns(GrammarAlgorithms.GroqApi);

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

        twitterSettingsOptionsMock.Value.Returns(twitterSettings);
        twitterLogServiceMock.GetLastTweetId().Returns(0);
        twitterClientMock.Users.GetFollowersAsync(twitterSettings.BotUsername)
            .Returns(new IUser[] { userMock });
        twitterClientMock.Timelines.GetUserTimelineAsync(Arg.Any<GetUserTimelineParameters>())
            .Returns(new ITweet[0]);
        twitterClientMock.Users.GetFriendIdsAsync(twitterSettings.BotUsername)
            .Returns(new long[0]);
        scheduleTweetServiceMock.GetPendingScheduledTweets()
            .Returns(Enumerable.Empty<ScheduledTweet>());
        grammarServiceMock.GrammarAlgorithm.Returns(GrammarAlgorithms.GroqApi);

        var hostedService = new TwitterBotHostedService(Substitute.For<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock }, twitterLogServiceMock, twitterClientMock, twitterSettingsOptionsMock, null, scheduleTweetServiceMock, null);

        // Act
        var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();
        await startTask;

        // Assert
        await twitterClientMock.Received().Timelines.GetUserTimelineAsync(Arg.Is<GetUserTimelineParameters>(g => g.PageSize == twitterSettings.TimelineFirstLoadPageSize));
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

        twitterSettingsOptionsMock.Value.Returns(twitterSettings);
        twitterLogServiceMock.GetLastTweetId().Returns(lastTweetId);
        twitterClientMock.Users.GetFollowersAsync(twitterSettings.BotUsername)
            .Returns(new IUser[] { userMock });
        twitterClientMock.Timelines.GetUserTimelineAsync(Arg.Any<GetUserTimelineParameters>())
            .Returns(new ITweet[0]);
        twitterClientMock.Users.GetFriendIdsAsync(twitterSettings.BotUsername)
            .Returns(new long[0]);
        scheduleTweetServiceMock.GetPendingScheduledTweets()
            .Returns(Enumerable.Empty<ScheduledTweet>());
        grammarServiceMock.GrammarAlgorithm.Returns(GrammarAlgorithms.GroqApi);

        var hostedService = new TwitterBotHostedService(Substitute.For<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock }, twitterLogServiceMock, twitterClientMock, twitterSettingsOptionsMock, null, scheduleTweetServiceMock, null);

        // Act
        var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();
        await startTask;

        // Assert
        await twitterClientMock.Received().Timelines.GetUserTimelineAsync(Arg.Is<GetUserTimelineParameters>(g => g.SinceId == lastTweetId));
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

        tweetMock.Text.Returns("RT @TwitterUser This is a tweet");

        twitterSettingsOptionsMock.Value.Returns(twitterSettings);
        twitterLogServiceMock.GetLastTweetId().Returns(lastTweetId);
        twitterClientMock.Users.GetFollowerIdsAsync(twitterSettings.BotUsername)
            .Returns(new long[] { 1 });
        twitterClientMock.Timelines.GetUserTimelineAsync(Arg.Any<GetUserTimelineParameters>())
            .Returns(new[] { tweetMock });
        twitterClientMock.Users.GetFriendIdsAsync(twitterSettings.BotUsername)
            .Returns(new long[0]);
        scheduleTweetServiceMock.GetPendingScheduledTweets()
            .Returns(Enumerable.Empty<ScheduledTweet>());
        grammarServiceMock.GrammarAlgorithm.Returns(GrammarAlgorithms.GroqApi);

        var hostedService = new TwitterBotHostedService(Substitute.For<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock }, twitterLogServiceMock, twitterClientMock, twitterSettingsOptionsMock, null, scheduleTweetServiceMock, null);

        // Act
        var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();
        await startTask;

        // Assert
        await grammarServiceMock.DidNotReceive().GetCorrections(Arg.Any<string>());
        await twitterLogServiceMock.DidNotReceive().LogTweet(Arg.Any<long>());
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

        tweetMock.Text.Returns("This is a tweet");

        twitterSettingsOptionsMock.Value.Returns(twitterSettings);
        twitterLogServiceMock.GetLastTweetId().Returns(lastTweetId);
        twitterClientMock.Users.GetFollowersAsync(twitterSettings.BotUsername)
            .Returns(new IUser[] { userMock });
        twitterClientMock.Timelines.GetUserTimelineAsync(Arg.Any<GetUserTimelineParameters>())
            .Returns(new[] { tweetMock });
        twitterClientMock.Users.GetFriendIdsAsync(twitterSettings.BotUsername)
            .Returns(new long[0]);
        scheduleTweetServiceMock.GetPendingScheduledTweets()
            .Returns(Enumerable.Empty<ScheduledTweet>());
        grammarServiceMock.GrammarAlgorithm.Returns(GrammarAlgorithms.GroqApi);

        grammarServiceMock.GetCorrections(tweetMock.Text)
            .Returns(new GrammarCheckResult(default));

        var hostedService = new TwitterBotHostedService(Substitute.For<ILogger<TwitterBotHostedService>>(), new[] { grammarServiceMock }, twitterLogServiceMock, twitterClientMock, twitterSettingsOptionsMock, null, scheduleTweetServiceMock, null);

        // Act
        var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();
        await startTask;

        // Assert
        await grammarServiceMock.Received().GetCorrections(tweetMock.Text); // Verify GetCorrections was called

        // Verify PublishTweetAsync was never called
        await twitterClientMock.DidNotReceive().Tweets.PublishTweetAsync(Arg.Any<IPublishTweetParameters>());
    }
}
