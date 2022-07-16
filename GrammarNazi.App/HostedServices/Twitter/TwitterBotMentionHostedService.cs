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

public class TwitterBotMentionHostedService : BaseTwitterHostedService
{
    private readonly IGrammarService _grammarService;
    private readonly ITwitterMentionLogService _twitterMentionLogService;
    private readonly ICatchExceptionService _catchExceptionService;

    public TwitterBotMentionHostedService(ILogger<TwitterBotHostedService> logger,
        IEnumerable<IGrammarService> grammarServices,
        ITwitterLogService twitterLogService,
        ITwitterClient twitterClient,
        IOptions<TwitterBotSettings> options,
        IScheduledTweetService scheduledTweetService,
        ITwitterMentionLogService twitterMentionLogService,
        ISentimentAnalysisService sentimentAnalysisService,
        ICatchExceptionService catchExceptionService)
        : base(logger, twitterLogService, twitterClient, options.Value, scheduledTweetService, sentimentAnalysisService)
    {
        _twitterMentionLogService = twitterMentionLogService;
        _grammarService = grammarServices.First(v => v.GrammarAlgorith == Defaults.DefaultAlgorithm);
        _grammarService.SetStrictnessLevel(CorrectionStrictnessLevels.Intolerant);
        _catchExceptionService = catchExceptionService;
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

                var myUser = await TwitterClient.Users.GetUserAsync(TwitterBotSettings.BotUsername);

                foreach (var mention in mentions.Where(x => x.Text.Contains($"@{TwitterBotSettings.BotUsername}") && (x.InReplyToStatusId.HasValue || x.QuotedStatusId.HasValue)))
                {
                    // Avoid correcting replies to my own tweets
                    if (mention.InReplyToUserId == myUser.Id)
                        continue;

                    var tweet = await GetTweetFromMention(mention);

                    if (tweet == null)
                        continue;

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
                _catchExceptionService.HandleException(ex, GithubIssueLabels.Twitter);
            }

            await Task.Delay(TwitterBotSettings.HostedServiceIntervalMilliseconds);
        }
    }

    private async Task<ITweet> GetTweetFromMention(ITweet mention)
    {
        try
        {
            return await TwitterClient.Tweets.GetTweetAsync(mention.InReplyToStatusId ?? mention.QuotedStatusId.Value);
        }
        catch (TwitterException ex) when (ex.ToString().Contains("blocked from the author of this tweet"))
        {
            Logger.LogWarning(ex, $"Blocked from {mention.CreatedBy.ScreenName}");
            return null;
        }
    }
}