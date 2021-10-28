using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface ITwitterMentionLogService
    {
        Task<long> GetLastTweetId();

        Task LogTweet(long tweetId, long replyTweetId);
    }
}