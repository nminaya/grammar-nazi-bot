using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using System;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class TwitterMentionLogService : ITwitterMentionLogService
    {
        private readonly IRepository<TwitterMentionLog> _repository;

        public TwitterMentionLogService(IRepository<TwitterMentionLog> repository)
        {
            _repository = repository;
        }

        public async Task<long> GetLastTweetId()
        {
            if (await _repository.Any())
            {
                return await _repository.Max(v => v.TweetId);
            }

            return 0;
        }

        public async Task LogTweet(long tweetId, long replyTweetId)
        {
            var twitterLog = new TwitterMentionLog
            {
                TweetId = tweetId,
                ReplyTweetId = replyTweetId,
                CreatedDate = DateTime.Now,
            };

            await _repository.Add(twitterLog);
        }
    }
}