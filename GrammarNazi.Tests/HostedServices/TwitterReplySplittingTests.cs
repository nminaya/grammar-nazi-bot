using GrammarNazi.App.HostedServices;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Entities.Configs;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using Tweetinvi.Parameters;
using Xunit;

namespace GrammarNazi.Tests.HostedServices;

public class TwitterReplySplittingTests
{
    [Fact]
    public async Task SplitReplyTweets_FirstReplyHasNoPrefix_SubsequentRepliesHavePrefix()
    {
        // Arrange
        var twitterClientMock = Substitute.For<ITwitterClient>();
        var twitterLogServiceMock = Substitute.For<ITwitterLogService>();
        var scheduleTweetServiceMock = Substitute.For<IScheduledTweetService>();
        var twitterSettingsOptionsMock = Substitute.For<IOptions<TwitterBotSettings>>();
        var grammarServiceMock = Substitute.For<IGrammarService>();
        var tweetMock = Substitute.For<ITweet>();
        var userMock = Substitute.For<IUser>();
        var cancellationTokenSource = new CancellationTokenSource();
        var twitterSettings = new TwitterBotSettings { BotUsername = "botUser", TimelineFirstLoadPageSize = 15, HostedServiceIntervalMilliseconds = 150 };

        userMock.ScreenName.Returns("authorUser");
        tweetMock.CreatedBy.Returns(userMock);
        tweetMock.Text.Returns("Sample text to check");
        tweetMock.Id.Returns(100L);
        tweetMock.UserMentions.Returns(new List<IUserMentionEntity>());

        twitterSettingsOptionsMock.Value.Returns(twitterSettings);
        twitterLogServiceMock.GetLastTweetId().Returns(123456L);
        twitterClientMock.Users.GetFollowersAsync(twitterSettings.BotUsername)
            .Returns(new IUser[] { userMock });
        twitterClientMock.Timelines.GetUserTimelineAsync(Arg.Any<GetUserTimelineParameters>())
            .Returns(new[] { tweetMock });
        twitterClientMock.Users.GetFriendIdsAsync(twitterSettings.BotUsername)
            .Returns(new long[0]);
        scheduleTweetServiceMock.GetPendingScheduledTweets()
            .Returns(Enumerable.Empty<ScheduledTweet>());
        grammarServiceMock.GrammarAlgorithm.Returns(GrammarAlgorithms.GroqApi);

        // Build corrections to ensure the resulting message exceeds TwitterTextMaxLength (280 chars)
        var corrections = Enumerable.Range(1, 15).Select(i => new GrammarCorrection
        {
            PossibleReplacements = new[] { $"replacement{i}" },
            Message = $"Correction detail message {i} explanation with additional padding text to make it long"
        }).ToList();

        var tcs = new TaskCompletionSource<bool>();
        grammarServiceMock.GetCorrections(Arg.Any<string>())
            .Returns(Task.FromResult(new GrammarCheckResult(corrections)));

        var publishedTweets = new List<IPublishTweetParameters>();
        twitterClientMock.Tweets.PublishTweetAsync(Arg.Do<IPublishTweetParameters>(p =>
        {
            publishedTweets.Add(p);
            if (publishedTweets.Count >= 2)
            {
                tcs.TrySetResult(true);
            }
        })).Returns(Task.FromResult(Substitute.For<ITweet>()));

        var hostedService = new TwitterBotHostedService(
            Substitute.For<ILogger<TwitterBotHostedService>>(),
            new[] { grammarServiceMock },
            twitterLogServiceMock,
            twitterClientMock,
            twitterSettingsOptionsMock,
            Substitute.For<IGithubService>(),
            scheduleTweetServiceMock,
            Substitute.For<ISentimentAnalysisService>());

        // Act
        var startTask = hostedService.StartAsync(cancellationTokenSource.Token);
        await tcs.Task;
        cancellationTokenSource.Cancel();
        await startTask;

        // Assert
        Assert.True(publishedTweets.Count >= 2, $"Expected at least 2 split reply tweets, but got {publishedTweets.Count}");

        // First tweet starts with @authorUser
        Assert.StartsWith("@authorUser", publishedTweets[0].Text);

        // Subsequent tweets also start with @authorUser
        for (int i = 1; i < publishedTweets.Count; i++)
        {
            Assert.StartsWith("@authorUser", publishedTweets[i].Text);
        }
    }
}
