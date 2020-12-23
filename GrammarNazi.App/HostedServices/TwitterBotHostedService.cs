using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.Configs;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace GrammarNazi.App.HostedServices
{
    public class TwitterBotHostedService : BackgroundService
    {
        private readonly ILogger<TwitterBotHostedService> _logger;
        private readonly ITwitterLogService _twitterLogService;
        private readonly IGrammarService _grammarService;
        private readonly ITwitterClient _twitterClient;
        private readonly IGithubService _githubService;
        private readonly TwitterBotSettings _twitterBotSettings;
        private readonly IScheduledTweetService _scheduledTweetService;

        public TwitterBotHostedService(ILogger<TwitterBotHostedService> logger,
            IEnumerable<IGrammarService> grammarServices,
            ITwitterLogService twitterLogService,
            ITwitterClient userClient,
            IOptions<TwitterBotSettings> options,
            IGithubService githubService,
            IScheduledTweetService scheduledTweetService)
        {
            _logger = logger;
            _twitterLogService = twitterLogService;
            _twitterClient = userClient;
            _githubService = githubService;
            _twitterBotSettings = options.Value;

            _grammarService = grammarServices.First(v => v.GrammarAlgorith == Defaults.DefaultAlgorithm);
            _grammarService.SetStrictnessLevel(CorrectionStrictnessLevels.Tolerant);
            _scheduledTweetService = scheduledTweetService;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TwitterBotHostedService started");
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var lastTweetIdTask = _twitterLogService.GetLastTweetId();

                    var followerIds = await _twitterClient.Users.GetFollowerIdsAsync(_twitterBotSettings.BotUsername);

                    long sinceTweetId = await lastTweetIdTask;

                    var tweets = new List<ITweet>();

                    foreach (var followerId in followerIds)
                    {
                        var getTimeLineParameters = new GetUserTimelineParameters(followerId);

                        if (sinceTweetId == 0)
                            getTimeLineParameters.PageSize = _twitterBotSettings.TimelineFirstLoadPageSize;
                        else
                            getTimeLineParameters.SinceId = sinceTweetId;

                        var timeLine = await _twitterClient.Timelines.GetUserTimelineAsync(getTimeLineParameters);

                        if (timeLine.Length == 0)
                            continue;

                        // Avoid Retweets.
                        tweets.AddRange(timeLine.Where(v => !v.Text.StartsWith("RT")));
                    }

                    foreach (var tweet in tweets)
                    {
                        var tweetText = StringUtils.RemoveHashtags(StringUtils.RemoveMentions(StringUtils.RemoveEmojis(tweet.Text)));

                        var correctionsResult = await _grammarService.GetCorrections(tweetText);

                        if (correctionsResult.HasCorrections)
                        {
                            var messageBuilder = new StringBuilder();

                            var mentionedUsers = tweet.UserMentions.Select(v => v.ToString()).Join(" "); // Other users mentioned in the tweet
                            messageBuilder.Append($"@{tweet.CreatedBy.ScreenName} {mentionedUsers}");

                            foreach (var correction in correctionsResult.Corrections)
                            {
                                // Only suggest the first possible replacement for now
                                messageBuilder.AppendLine($"*{correction.PossibleReplacements.First()} [{correction.Message}]");
                            }

                            var correctionString = messageBuilder.ToString();

                            _logger.LogInformation($"Sending reply to: {tweet.CreatedBy.ScreenName}");
                            var publishTweetParameters = new PublishTweetParameters(correctionString)
                            {
                                InReplyToTweetId = tweet.Id
                            };
                            var replyTweet = await _twitterClient.Tweets.PublishTweetAsync(publishTweetParameters);

                            if (replyTweet != null)
                            {
                                _logger.LogInformation("Reply sent successfuly");
                                await _twitterLogService.LogReply(tweet.Id, replyTweet.Id);
                            }

                            await Task.Delay(_twitterBotSettings.PublishTweetDelayMilliseconds);
                        }
                    }

                    var followBackUsersTask = FollowBackUsers(followerIds);
                    var publishScheduledTweetsTask = PublishScheduledTweets();

                    if (tweets.Any())
                    {
                        var lastTweet = tweets.OrderByDescending(v => v.Id).First();

                        // Save last Tweet Id
                        await _twitterLogService.LogTweet(lastTweet.Id);
                    }

                    await followBackUsersTask;
                    await publishScheduledTweetsTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);

                    // fire and forget
                    _ = _githubService.CreateBugIssue($"Application Exception: {ex.Message}", ex);
                }

                await Task.Delay(_twitterBotSettings.HostedServiceIntervalMilliseconds);
            }
        }

        private async Task FollowBackUsers(IEnumerable<long> followerdIds)
        {
            var friendIds = await _twitterClient.Users.GetFriendIdsAsync(_twitterBotSettings.BotUsername);

            var userIdsToFollow = followerdIds.Except(friendIds);

            foreach (var userId in userIdsToFollow)
            {
                await _twitterClient.Users.FollowUserAsync(userId);
            }
        }

        private async Task PublishScheduledTweets()
        {
            var scheduledTweets = await _scheduledTweetService.GetPendingScheduledTweets();

            foreach (var scheduledTweet in scheduledTweets)
            {
                var tweet = await _twitterClient.Tweets.PublishTweetAsync(scheduledTweet.TweetText);

                if (tweet != null)
                {
                    scheduledTweet.TweetId = tweet.Id;
                    scheduledTweet.IsPublished = true;
                    scheduledTweet.PublishDate = DateTime.Now;

                    await _scheduledTweetService.Update(scheduledTweet);
                    await Task.Delay(_twitterBotSettings.PublishTweetDelayMilliseconds);
                }
            }
        }
    }
}