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
using Tweetinvi.Parameters;

namespace GrammarNazi.App.HostedServices
{
    public class TwitterBotMentionHostedService : BaseTwitterHostedService
    {
        private readonly IGrammarService _grammarService;
        private readonly IGithubService _githubService;
        private readonly ITwitterMentionLogService _twitterMentionLogService;

        public TwitterBotMentionHostedService(ILogger<TwitterBotHostedService> logger,
            IEnumerable<IGrammarService> grammarServices,
            ITwitterLogService twitterLogService,
            ITwitterClient twitterClient,
            IOptions<TwitterBotSettings> options,
            IGithubService githubService,
            IScheduledTweetService scheduledTweetService,
            ITwitterMentionLogService twitterMentionLogService,
            ISentimentAnalysisService sentimentAnalysisService)
            : base(logger, twitterLogService, twitterClient, options.Value, scheduledTweetService, sentimentAnalysisService)
        {
            _githubService = githubService;
            _twitterMentionLogService = twitterMentionLogService;
            _grammarService = grammarServices.First(v => v.GrammarAlgorith == Defaults.DefaultAlgorithm);
            _grammarService.SetStrictnessLevel(CorrectionStrictnessLevels.Intolerant);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("TwitterBotMentionHostedService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    long lastTweetId = await _twitterMentionLogService.GetLastTweetId();

                    var getMentionParameters = new GetMentionsTimelineParameters();

                    if (lastTweetId == 0)
                        getMentionParameters.PageSize = TwitterBotSettings.TimelineFirstLoadPageSize;
                    else
                        getMentionParameters.SinceId = lastTweetId;

                    var mentions = await TwitterClient.Timelines.GetMentionsTimelineAsync(getMentionParameters);

                    foreach (var mention in mentions.Where(x => x.InReplyToStatusId.HasValue))
                    {
                        var tweet = await TwitterClient.Tweets.GetTweetAsync(mention.InReplyToStatusId.Value);

                        var tweetText = StringUtils.RemoveHashtags(StringUtils.RemoveMentions(StringUtils.RemoveEmojis(tweet.Text)));

                        var correctionsResult = await _grammarService.GetCorrections(tweetText);

                        if (!correctionsResult.HasCorrections)
                        {
                            await PublishReplyTweet($"@{mention.CreatedBy.ScreenName} I don't see anything wrong here.", mention.Id);
                            continue;
                        }

                        var messageBuilder = new StringBuilder();

                        messageBuilder.Append($"@{mention.CreatedBy.ScreenName}");

                        foreach (var correction in correctionsResult.Corrections)
                        {
                            // Only suggest the first possible replacement
                            messageBuilder.AppendLine($"*{correction.PossibleReplacements.First()} [{correction.Message}]");
                        }

                        var correctionString = messageBuilder.ToString();

                        Logger.LogInformation($"Sending reply to: {mention.CreatedBy.ScreenName}");

                        if (correctionString.Length >= Defaults.TwitterTextMaxLength)
                        {
                            var replyTweets = correctionString.SplitInParts(Defaults.TwitterTextMaxLength);

                            foreach (var (reply, index) in replyTweets.WithIndex())
                            {
                                var correctionStringSplitted = index == 0 ? reply : $"@{mention.CreatedBy.ScreenName} {reply}";

                                await PublishReplyTweet(correctionStringSplitted, mention.Id);

                                await Task.Delay(TwitterBotSettings.PublishTweetDelayMilliseconds, stoppingToken);
                            }

                            continue;
                        }

                        await PublishReplyTweet(correctionString, mention.Id);

                        await Task.Delay(TwitterBotSettings.PublishTweetDelayMilliseconds, stoppingToken);
                    }

                    if (mentions.Any())
                    {
                        var lastTweet = mentions.OrderByDescending(v => v.Id).First();

                        // Save last Tweet Id
                        await _twitterMentionLogService.LogTweet(lastTweet.Id, default);
                    }

                    var followersTask = TwitterClient.Users.GetFollowersAsync(TwitterBotSettings.BotUsername);
                    var friendIdsTask = TwitterClient.Users.GetFriendIdsAsync(TwitterBotSettings.BotUsername);

                    await FollowBackUsers(await followersTask, await friendIdsTask);
                    await PublishScheduledTweets();
                    await LikeRepliesToBot(mentions);
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
}