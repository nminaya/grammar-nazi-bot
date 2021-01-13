using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using System;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class TwitterLogService : ITwitterLogService
    {
        private readonly IRepository<TwitterLog> _repository;

        public TwitterLogService(IRepository<TwitterLog> repository)
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

        public async Task LogReply(long tweetId, long replyTweetId)
        {
            var twitterLog = new TwitterLog
            {
                TweetId = tweetId,
                ReplyTweetId = replyTweetId,
                CreatedDate = DateTime.Now
            };

            await _repository.Add(twitterLog);
        }

        public async Task LogTweet(long tweetId)
        {
            if (await TweetExist(tweetId))
                return;

            var twitterLog = new TwitterLog
            {
                TweetId = tweetId,
                CreatedDate = DateTime.Now
            };

            await _repository.Add(twitterLog);
        }

        public Task<bool> TweetExist(long tweetId)
        {
            return _repository.Any(v => v.TweetId == tweetId);
        }
    }
}