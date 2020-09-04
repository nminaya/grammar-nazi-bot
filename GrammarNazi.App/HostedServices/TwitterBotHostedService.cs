using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        public TwitterBotHostedService(ILogger<TwitterBotHostedService> logger,
            IEnumerable<IGrammarService> grammarServices,
            ITwitterLogService twitterLogService,
            ITwitterClient userClient)
        {
            _logger = logger;
            _twitterLogService = twitterLogService;
            _twitterClient = userClient;

            // Use only DefaultAlgorithm for now
            _grammarService = grammarServices.First(v => v.GrammarAlgorith == Defaults.DefaultAlgorithm);
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
                    _logger.LogInformation("Getting tweets from followers");

                    _logger.LogInformation("Getting followers");
                    var user = await _twitterClient.Users.GetUserAsync("GrammarNazi_Bot"); // TODO: Get bot name from config

                    var followerIdsIterator = user.GetFollowerIds();

                    var followers = new List<IUser>();

                    var lastTweetIdTask = _twitterLogService.GetLastTweetId();

                    while (!followerIdsIterator.Completed)
                    {
                        var page = await followerIdsIterator.NextPageAsync();

                        foreach (var followerId in page)
                        {
                            followers.Add(await _twitterClient.Users.GetUserAsync(followerId));
                        }
                    }

                    _logger.LogInformation($"Followers: {followers.Count}");

                    var tweets = new List<ITweet>();

                    long sinceTweetId = await lastTweetIdTask;

                    foreach (var follower in followers)
                    {
                        _logger.LogInformation($"Getting TimeLine of {follower.ScreenName}");

                        var getTimeLineParameters = new GetUserTimelineParameters(follower.Id);

                        if (sinceTweetId == 0)
                            getTimeLineParameters.PageSize = 5; // TODO: Get this value from config
                        else
                            getTimeLineParameters.SinceId = sinceTweetId;

                        var timeLine = await _twitterClient.Timelines.GetUserTimelineAsync(getTimeLineParameters);

                        if (timeLine.Length == 0)
                        {
                            _logger.LogInformation($"No new tweets for {follower.ScreenName}");
                            continue;
                        }

                        tweets.AddRange(timeLine.Where(v => !v.Text.StartsWith("RT"))); // Avoid Retweets
                    }

                    foreach (var tweet in tweets)
                    {
                        var correctionsResult = await _grammarService.GetCorrections(tweet.Text);

                        if (correctionsResult.HasCorrections)
                        {
                            var messageBuilder = new StringBuilder();

                            messageBuilder.Append($"@{tweet.CreatedBy.ScreenName} {tweet.Prefix}");

                            foreach (var correction in correctionsResult.Corrections)
                            {
                                // Only suggest the first possible replacement for now
                                messageBuilder.AppendLine($"*{correction.PossibleReplacements.First()}");
                            }

                            var correctionString = messageBuilder.ToString();

                            _logger.LogInformation($"Sending reply to: {tweet.CreatedBy.ScreenName}");
                            var publishTweetParameters = new PublishTweetParameters(correctionString)
                            {
                                InReplyToTweetId = tweet.Id,
                            };
                            var replyTweet = await _twitterClient.Tweets.PublishTweetAsync(publishTweetParameters);

                            if (replyTweet != null)
                            {
                                _logger.LogInformation("Reply sent successfuly");
                                await _twitterLogService.LogTweet(tweet.Id, replyTweet.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                // TODO: Get this value from config
                // Wait 10 minutes to execute again
                await Task.Delay(10 * 60_000);
            }
        }
    }
}