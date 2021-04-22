using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class DiscordChannelConfigService : IDiscordChannelConfigService
    {
        private readonly IRepository<DiscordChannelConfig> _repository;

        public DiscordChannelConfigService(IRepository<DiscordChannelConfig> repository)
        {
            _repository = repository;
        }

        public Task AddConfiguration(DiscordChannelConfig channelConfig)
        {
            return _repository.Add(channelConfig);
        }

        public Task Delete(DiscordChannelConfig channelConfig)
        {
            return _repository.Delete(channelConfig);
        }

        public Task<DiscordChannelConfig> GetConfigurationByChannelId(ulong channelId)
        {
            return _repository.GetFirst(v => v.ChannelId == channelId);
        }

        public async Task Update(DiscordChannelConfig channelConfig)
        {
            await _repository.Update(channelConfig, v => v.ChannelId == channelConfig.ChannelId);
        }
    }
}