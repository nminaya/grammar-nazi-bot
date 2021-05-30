using System;

namespace GrammarNazi.Domain.Entities
{
    public class TwitterMentionLog
    {
        /// <summary>
        /// Id of Twwet
        /// </summary>
        public long TweetId { get; set; }

        /// <summary>
        /// Id of replied Tweet
        /// </summary>
        public long ReplyTweetId { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreatedDate { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is TwitterMentionLog twitterLog)
                return TweetId == twitterLog.TweetId;

            return false;
        }

        public override int GetHashCode()
        {
            return TweetId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{TweetId}";
        }
    }
}