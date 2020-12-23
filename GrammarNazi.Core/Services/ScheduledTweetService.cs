using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
	public class ScheduledTweetService : IScheduledTweetService
	{
		private readonly IRepository<ScheduledTweet> _repository;

		public ScheduledTweetService(IRepository<ScheduledTweet> repository)
		{
			_repository = repository;
		}

		public Task Add(ScheduledTweet scheduledTweet)
		{
			return _repository.Add(scheduledTweet);
		}

		public Task<IEnumerable<ScheduledTweet>> GetPendingScheduledTweets()
		{
			return _repository.GetAll(v => v.PublishAfter < DateTime.Now);
		}

		public Task Update(ScheduledTweet scheduledTweet)
		{
			return _repository.Update(scheduledTweet, v => v.Id == scheduledTweet.Id);
		}
	}
}