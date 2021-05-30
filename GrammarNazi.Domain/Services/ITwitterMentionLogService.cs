using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface ITwitterMentionLogService
    {
        Task<long> GetLastTweetId();

        Task LogTweet(long tweetId, long replyTweetId);
    }
}
