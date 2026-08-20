using GrammarNazi.Domain.Entities;

namespace GrammarNazi.Domain.Services;

public interface IScheduledTweetService
{
    Task<IEnumerable<ScheduledTweet>> GetPendingScheduledTweets();

    Task Update(ScheduledTweet scheduledTweet);

    Task Add(ScheduledTweet scheduledTweet);
}
