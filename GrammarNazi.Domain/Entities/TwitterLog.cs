using System;

namespace GrammarNazi.Domain.Entities
{
    public class TwitterLog
    {
        public long TweetId { get; set; }
        public long ReplyTweetId { get; set; }
        public DateTime CreatedDate { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is TwitterLog twitterLog)
                return TweetId == twitterLog.TweetId;

            return base.Equals(obj);
        }
    }
}