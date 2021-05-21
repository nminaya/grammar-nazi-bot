using System;

namespace GrammarNazi.Domain.Entities
{
    /// <summary>
    /// Twitter Log
    /// </summary>
    public class TwitterLog
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
        /// Text of the tweeted tweet
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreatedDate { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is TwitterLog twitterLog)
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