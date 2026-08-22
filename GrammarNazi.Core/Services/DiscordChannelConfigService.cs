using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Caching.Memory;

namespace GrammarNazi.Core.Services;

public class DiscordChannelConfigService(IRepository<DiscordChannelConfig> repository, IMemoryCache memoryCache) : IDiscordChannelConfigService
{
    public async Task AddConfiguration(DiscordChannelConfig channelConfig)
    {
        await repository.Add(channelConfig);
        memoryCache.Set(GetCacheKey(channelConfig.ChannelId), channelConfig, TimeSpan.FromHours(2));
    }

    public async Task Delete(DiscordChannelConfig channelConfig)
    {
        await repository.Delete(channelConfig);
        memoryCache.Remove(GetCacheKey(channelConfig.ChannelId));
    }

    public async Task<DiscordChannelConfig> GetConfigurationByChannelId(ulong channelId)
    {
        if (memoryCache.TryGetValue(GetCacheKey(channelId), out DiscordChannelConfig channelConfig))
        {
            return channelConfig;
        }

        channelConfig = await repository.GetFirst(v => v.ChannelId == channelId);

        if (channelConfig != null)
        {
            memoryCache.Set(GetCacheKey(channelId), channelConfig, TimeSpan.FromHours(2));
        }

        return channelConfig;
    }

    public async Task Update(DiscordChannelConfig channelConfig)
    {
        await repository.Update(channelConfig, v => v.ChannelId == channelConfig.ChannelId);
        memoryCache.Set(GetCacheKey(channelConfig.ChannelId), channelConfig, TimeSpan.FromHours(2));
    }

    private static string GetCacheKey(ulong channelId) => $"{nameof(DiscordChannelConfig)}_{channelId}";
}
