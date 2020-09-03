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
        private readonly IGrammarService _grammarService;
        private readonly ITwitterClient _twitterClient;

        // TODO: Save this value using a repository
        // It will contain the last tweet the bot replied
        private static long _lastTweetId = 0;

        public TwitterBotHostedService(ILogger<TwitterBotHostedService> logger,
            IEnumerable<IGrammarService> grammarServices,
            ITwitterClient userClient)
        {
            _logger = logger;
            _twitterClient = userClient;

            // Use only DefaultAlgorithm for now
            _grammarService = grammarServices.First(v => v.GrammarAlgorith == Defaults.DefaultAlgorithm);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"Getting tweets from followers");

                    _logger.LogInformation($"Getting followers");
                    var user = await _twitterClient.Users.GetUserAsync("GrammarNazi_Bot"); // TODO: Get bot name from config

                    var followerIds = user.GetFollowerIds();

                    var followers = new List<IUser>();

                    while (!followerIds.Completed)
                    {
                        var page = await followerIds.NextPageAsync();
                        foreach (var item in page)
                        {
                            followers.Add(await _twitterClient.Users.GetUserAsync(item));
                        }
                    }

                    _logger.LogInformation($"Followers: {followers.Count}");

                    var tweetList = new List<ITweet>();

                    foreach (var follower in followers)
                    {
                        _logger.LogInformation($"Getting TimeLine of {follower.ScreenName}");

                        var getTimeLineParameters = new GetUserTimelineParameters(follower.Id)
                        {
                            SinceId = _lastTweetId == 0 ? (long?)null : _lastTweetId,
                            PageSize = 5 // TODO: Get this value from config
                        };
                        var timeLine = await _twitterClient.Timelines.GetUserTimelineAsync(getTimeLineParameters);

                        if (timeLine.Length == 0)
                        {
                            _logger.LogInformation($"No new tweets for {follower.ScreenName}");
                            continue;
                        }

                        tweetList.AddRange(timeLine);
                    }

                    foreach (var tweet in tweetList.Where(v => !v.Text.StartsWith("RT"))) // Avoid Retweets
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
                                _lastTweetId = tweet.Id;
                                _logger.LogInformation("Reply sent successfuly");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.ToString());
                }

                // TODO: Get this value from config
                // Wait 10 minutes
                await Task.Delay(10 * 60_000);
            }
        }
    }
}