using System;

namespace GrammarNazi.Domain.Entities
{
	/// <summary>
	/// Class that holds information about scheduled tweet
	/// </summary>
	public class ScheduledTweet
	{
		/// <summary>
		/// Identifier
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// True if tweet is published
		/// </summary>
		public bool IsPublished { get; set; }

		/// <summary>
		/// Date after publish the tweet
		/// </summary>
		public DateTime PublishAfter { get; set; }

		/// <summary>
		/// Tweet Text
		/// </summary>
		public string TweetText { get; set; }

		/// <summary>
		/// Tweet id of the published Tweet
		/// </summary>
		public int? TweetId { get; set; }

		/// <summary>
		/// Date of tweet published
		/// </summary>
		public DateTime? PublishDate { get; set; }

		public ScheduledTweet()
		{
			Id = Guid.NewGuid();
		}

		public override bool Equals(object obj)
		{
			if (obj is ScheduledTweet scheduledTweet)
				return Id == scheduledTweet.Id;

			return false;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public override string ToString()
		{
			return $"{Id}";
		}
	}
}