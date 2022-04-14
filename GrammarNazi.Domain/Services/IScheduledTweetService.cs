using GrammarNazi.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services;

public interface IScheduledTweetService
{
    Task<IEnumerable<ScheduledTweet>> GetPendingScheduledTweets();

    Task Update(ScheduledTweet scheduledTweet);

    Task Add(ScheduledTweet scheduledTweet);
}
