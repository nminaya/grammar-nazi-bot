using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.Configs;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Exceptions;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace GrammarNazi.App.HostedServices;

/// <summary>
/// Disabled due to Twitter limits and policy. Using TwitterBotMentionHostedService instead.
/// </summary>
public class TwitterBotHostedService : BaseTwitterHostedService
{
    private readonly IGrammarService _grammarService;
    private readonly IGithubService _githubService;

    public TwitterBotHostedService(ILogger<TwitterBotHostedService> logger,
        IEnumerable<IGrammarService> grammarServices,
        ITwitterLogService twitterLogService,
        ITwitterClient userClient,
        IOptions<TwitterBotSettings> options,
        IGithubService githubService,
        IScheduledTweetService scheduledTweetService,
        ISentimentAnalysisService sentimentAnalysisService)
        : base(logger, twitterLogService, userClient, options.Value, scheduledTweetService, sentimentAnalysisService)
    {
        _githubService = githubService;

        _grammarService = grammarServices.First(v => v.GrammarAlgorith == Defaults.DefaultAlgorithm);
        _grammarService.SetStrictnessLevel(CorrectionStrictnessLevels.Tolerant);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("TwitterBotHostedService started");
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var lastTweetIdTask = TwitterLogService.GetLastTweetId();

                var followers = await TwitterClient.Users.GetFollowersAsync(TwitterBotSettings.BotUsername);

                var friendIds = await TwitterClient.Users.GetFriendIdsAsync(TwitterBotSettings.BotUsername);

                long sinceTweetId = await lastTweetIdTask;

                var tweets = new List<ITweet>();

                foreach (var follower in followers)
                {
                    if (follower.Protected && !friendIds.Contains(follower.Id))
                        continue;

                    var getTimeLineParameters = new GetUserTimelineParameters(follower);

                    if (sinceTweetId == 0)
                        getTimeLineParameters.PageSize = TwitterBotSettings.TimelineFirstLoadPageSize;
                    else
                        getTimeLineParameters.SinceId = sinceTweetId;

                    var timeLine = await TwitterClient.Timelines.GetUserTimelineAsync(getTimeLineParameters);

                    if (timeLine.Length == 0)
                        continue;

                    // Avoid Retweets.
                    tweets.AddRange(timeLine.Where(v => !v.Text.StartsWith("RT")));
                }

                foreach (var tweet in tweets)
                {
                    var tweetText = StringUtils.RemoveHashtags(StringUtils.RemoveMentions(StringUtils.RemoveEmojis(tweet.Text)));

                    var correctionsResult = await _grammarService.GetCorrections(tweetText);

                    if (!correctionsResult.HasCorrections)
                        continue;

                    var messageBuilder = new StringBuilder();

                    var mentionedUsers = tweet.UserMentions.Select(v => v.ToString()).Join(" "); // Other users mentioned in the tweet
                    messageBuilder.Append($"@{tweet.CreatedBy.ScreenName} {mentionedUsers} ");

                    foreach (var correction in correctionsResult.Corrections)
                    {
                        // Only suggest the first possible replacement
                        messageBuilder.AppendLine($"*{correction.PossibleReplacements.First()} [{correction.Message}]");
                    }

                    var correctionString = messageBuilder.ToString();

                    Logger.LogInformation($"Sending reply to: {tweet.CreatedBy.ScreenName}");

                    if (correctionString.Length >= Defaults.TwitterTextMaxLength)
                    {
                        var replyTweets = correctionString.SplitInParts(Defaults.TwitterTextMaxLength);

                        foreach (var (reply, index) in replyTweets.WithIndex())
                        {
                            var correctionStringSplitted = index == 0 ? reply : $"@{tweet.CreatedBy.ScreenName} {mentionedUsers} {reply}";

                            await PublishReplyTweet(correctionStringSplitted, tweet.Id);

                            await Task.Delay(TwitterBotSettings.PublishTweetDelayMilliseconds, stoppingToken);
                        }

                        continue;
                    }

                    await PublishReplyTweet(correctionString, tweet.Id);

                    await Task.Delay(TwitterBotSettings.PublishTweetDelayMilliseconds, stoppingToken);
                }

                if (tweets.Any())
                {
                    var lastTweet = tweets.OrderByDescending(v => v.Id).First();

                    // Save last Tweet Id
                    await TwitterLogService.LogTweet(lastTweet.Id);
                }

                await FollowBackUsers(followers, friendIds);
                await PublishScheduledTweets();
                await LikeRepliesToBot(tweets);
            }
            catch (Exception ex)
            {
                var message = ex is TwitterException tEx ? tEx.TwitterDescription : ex.Message;

                Logger.LogError(ex, message);

                // fire and forget
                _ = _githubService.CreateBugIssue($"Application Exception: {message}", ex, GithubIssueLabels.Twitter);
            }

            await Task.Delay(TwitterBotSettings.HostedServiceIntervalMilliseconds);
        }
    }
}