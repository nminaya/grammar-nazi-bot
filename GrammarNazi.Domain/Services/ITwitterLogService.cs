using System;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface ITwitterLogService
    {
        Task<long> GetLastTweetId();

        Task LogReply(long tweetId, long replyTweetId, string tweetText);

        Task LogTweet(long tweetId, string tweetText);

        Task<bool> ReplyTweetExist(long tweetId);

        Task<bool> TweetExist(string tweetText, DateTime createdAfter);
    }
}