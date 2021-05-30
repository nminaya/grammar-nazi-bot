using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.Configs;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Exceptions;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace GrammarNazi.App.HostedServices
{
    public abstract class BaseTwitterHostedService : BackgroundService
    {
        protected readonly ILogger<TwitterBotHostedService> Logger;
        protected readonly ITwitterLogService TwitterLogService;
        protected readonly ITwitterClient TwitterClient;
        protected readonly TwitterBotSettings TwitterBotSettings;
        protected readonly IScheduledTweetService ScheduledTweetService;
        protected readonly ISentimentAnalysisService SentimentAnalysisService;

        protected BaseTwitterHostedService(ILogger<TwitterBotHostedService> logger,
            ITwitterLogService twitterLogService,
            ITwitterClient twitterClient,
            TwitterBotSettings twitterBotSettings,
            IScheduledTweetService scheduledTweetService,
            ISentimentAnalysisService sentimentAnalysisService)
        {
            Logger = logger;
            TwitterLogService = twitterLogService;
            TwitterClient = twitterClient;
            TwitterBotSettings = twitterBotSettings;
            ScheduledTweetService = scheduledTweetService;
            SentimentAnalysisService = sentimentAnalysisService;
        }

        protected async Task FollowBackUsers(IEnumerable<IUser> followers, IEnumerable<long> friendIds)
        {
            var userIdsPendingToFollow = await TwitterClient.Users.GetUserIdsYouRequestedToFollowAsync();

            var userIdsToFollow = followers.Select(v => v.Id).Except(friendIds).Except(userIdsPendingToFollow);

            foreach (var userId in userIdsToFollow)
            {
                await TwitterClient.Users.FollowUserAsync(userId);
            }
        }

        protected async Task PublishScheduledTweets()
        {
            var scheduledTweets = await ScheduledTweetService.GetPendingScheduledTweets();

            foreach (var scheduledTweet in scheduledTweets)
            {
                var tweet = await TwitterClient.Tweets.PublishTweetAsync(scheduledTweet.TweetText);

                if (tweet == null)
                {
                    Logger.LogWarning($"Not able to tweet Schedule Tweet {scheduledTweet}");
                    continue;
                }

                scheduledTweet.TweetId = tweet.Id;
                scheduledTweet.IsPublished = true;
                scheduledTweet.PublishDate = DateTime.Now;

                await ScheduledTweetService.Update(scheduledTweet);
                await Task.Delay(TwitterBotSettings.PublishTweetDelayMilliseconds);
            }
        }

        protected async Task LikeRepliesToBot(List<ITweet> tweets)
        {
            var replies = tweets.Where(v => v.InReplyToStatusId != null);

            foreach (var reply in replies)
            {
                bool isReplyToBot = await TwitterLogService.ReplyTweetExist(reply.InReplyToStatusId.Value);

                if (!isReplyToBot)
                    continue;

                var sentimentAnalysis = await SentimentAnalysisService.GetSentimentAnalysis(reply.Text);

                if (sentimentAnalysis.Type == SentimentTypes.Positive
                    && sentimentAnalysis.Score >= Defaults.ValidPositiveSentimentScore)
                {
                    await TwitterClient.Tweets.FavoriteTweetAsync(reply.Id);
                }
            }
        }

        protected async Task PublishReplyTweet(string text, long replyTo)
        {
            try
            {
                var tweetText = StringUtils.RemoveMentions(text);

                bool tweetExist = await TwitterLogService.TweetExist(tweetText, DateTime.Now.AddHours(-TwitterBotSettings.PublishRepeatedTweetAfterHours));

                if (tweetExist)
                {
                    // Avoid tweeting the same tweet
                    // TODO: Find out what to do in this scenario
                    // #231: https://github.com/nminaya/grammar-nazi-bot/issues/231
                    Logger.LogWarning($"Attempt to publish a duplicate reply: {text}");
                    return;
                }

                var publishTweetsParameters = new PublishTweetParameters(text)
                {
                    InReplyToTweetId = replyTo
                };
                var replyTweet = await TwitterClient.Tweets.PublishTweetAsync(publishTweetsParameters);

                if (replyTweet == null)
                {
                    Logger.LogWarning("Not able to tweet Reply", text, replyTo);
                    return;
                }

                Logger.LogInformation("Reply sent successfuly");
                await TwitterLogService.LogReply(replyTweet.Id, replyTo, replyTweet.Text);
            }
            catch (TwitterException ex) when (ex.ToString().Contains("The original Tweet author restricted who can reply to this Tweet"))
            {
                Logger.LogWarning($"The author restricted who can reply to this tweet {replyTo}");
            }
        }
    }
}