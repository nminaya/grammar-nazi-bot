using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class ChatConfigurationService : IChatConfigurationService
    {
        private readonly IRepository<ChatConfiguration> _repository;

        public ChatConfigurationService(IRepository<ChatConfiguration> repository)
        {
            _repository = repository;
        }

        public Task AddConfiguration(ChatConfiguration chatConfiguration)
        {
            return _repository.Add(chatConfiguration);
        }

        public Task Delete(ChatConfiguration chatConfiguration)
        {
            return _repository.Delete(chatConfiguration);
        }

        public Task<ChatConfiguration> GetConfigurationByChatId(long chatId)
        {
            return _repository.GetFirst(v => v.ChatId == chatId);
        }

        public async Task Update(ChatConfiguration chatConfiguration)
        {
            await _repository.Update(chatConfiguration, v => v.ChatId == chatConfiguration.ChatId);
        }
    }
}