using GrammarNazi.Domain.Entities.Configs;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Parameters;

namespace GrammarNazi.App.HostedServices
{
    public class TwitterBotMentionHostedService : TwitterBotHostedService
    {
        private readonly ITwitterClient _twitterClient;

        public TwitterBotMentionHostedService(ILogger<TwitterBotHostedService> logger,
            IEnumerable<IGrammarService> grammarServices,
            ITwitterLogService twitterLogService,
            ITwitterClient twitterClient,
            IOptions<TwitterBotSettings> options,
            IGithubService githubService,
            IScheduledTweetService scheduledTweetService,
            ISentimentAnalysisService sentimentAnalysisService)
            : base(logger, grammarServices, twitterLogService, twitterClient, options, githubService, scheduledTweetService, sentimentAnalysisService)
        {
            _twitterClient = twitterClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // TODO: Get last tweet mention ID
                    long lastTweetId = 0;

                    var getMentionParameters = new GetMentionsTimelineParameters();

                    var mentions = await _twitterClient.Timelines.GetMentionsTimelineAsync(getMentionParameters);
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}