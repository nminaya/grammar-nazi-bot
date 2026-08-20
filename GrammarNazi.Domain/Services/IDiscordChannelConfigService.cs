using GrammarNazi.Domain.Entities;

namespace GrammarNazi.Domain.Services;

public interface IDiscordChannelConfigService
{
    Task AddConfiguration(DiscordChannelConfig channelConfig);

    Task Delete(DiscordChannelConfig channelConfig);

    Task Update(DiscordChannelConfig channelConfig);

    Task<DiscordChannelConfig> GetConfigurationByChannelId(ulong channelId);
}
