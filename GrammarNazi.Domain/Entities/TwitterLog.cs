using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities
{
    public class TwitterLog
    {
        public long TweetId { get; set; }
        public long ReplyTweetId { get; set; }
    }
}
