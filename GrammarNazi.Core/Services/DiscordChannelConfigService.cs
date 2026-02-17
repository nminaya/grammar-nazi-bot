using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services;

public class DiscordChannelConfigService : IDiscordChannelConfigService
{
    private readonly IRepository<DiscordChannelConfig> _repository;
    private readonly IMemoryCache _memoryCache;

    public DiscordChannelConfigService(IRepository<DiscordChannelConfig> repository, IMemoryCache memoryCache)
    {
        _repository = repository;
        _memoryCache = memoryCache;
    }

    public async Task AddConfiguration(DiscordChannelConfig channelConfig)
    {
        await _repository.Add(channelConfig);
        _memoryCache.Set(GetCacheKey(channelConfig.ChannelId), channelConfig, TimeSpan.FromHours(2));
    }

    public async Task Delete(DiscordChannelConfig channelConfig)
    {
        await _repository.Delete(channelConfig);
        _memoryCache.Remove(GetCacheKey(channelConfig.ChannelId));
    }

    public async Task<DiscordChannelConfig> GetConfigurationByChannelId(ulong channelId)
    {
        if (_memoryCache.TryGetValue(GetCacheKey(channelId), out DiscordChannelConfig channelConfig))
        {
            return channelConfig;
        }

        channelConfig = await _repository.GetFirst(v => v.ChannelId == channelId);

        if (channelConfig != null)
        {
            _memoryCache.Set(GetCacheKey(channelId), channelConfig, TimeSpan.FromHours(2));
        }

        return channelConfig;
    }

    public async Task Update(DiscordChannelConfig channelConfig)
    {
        await _repository.Update(channelConfig, v => v.ChannelId == channelConfig.ChannelId);
        _memoryCache.Set(GetCacheKey(channelConfig.ChannelId), channelConfig, TimeSpan.FromHours(2));
    }

    private static string GetCacheKey(ulong channelId) => $"{nameof(DiscordChannelConfig)}_{channelId}";
}