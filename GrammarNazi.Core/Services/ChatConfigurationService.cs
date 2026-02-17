using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services;

public class ChatConfigurationService : IChatConfigurationService
{
    private readonly IRepository<ChatConfiguration> _repository;
    private readonly IMemoryCache _memoryCache;

    public ChatConfigurationService(IRepository<ChatConfiguration> repository, IMemoryCache memoryCache)
    {
        _repository = repository;
        _memoryCache = memoryCache;
    }

    public async Task AddConfiguration(ChatConfiguration chatConfiguration)
    {
        if (await _repository.Any(x => x.ChatId == chatConfiguration.ChatId))
        {
            await Update(chatConfiguration);
            return;
        }

        await _repository.Add(chatConfiguration);
        _memoryCache.Set(GetCacheKey(chatConfiguration.ChatId), chatConfiguration, TimeSpan.FromHours(2));
    }

    public async Task Delete(ChatConfiguration chatConfiguration)
    {
        await _repository.Delete(chatConfiguration);
        _memoryCache.Remove(GetCacheKey(chatConfiguration.ChatId));
    }

    public async Task<ChatConfiguration> GetConfigurationByChatId(long chatId)
    {
        if (_memoryCache.TryGetValue(GetCacheKey(chatId), out ChatConfiguration chatConfig))
        {
            return chatConfig;
        }

        chatConfig = await _repository.GetFirst(v => v.ChatId == chatId);

        if (chatConfig != null)
        {
            _memoryCache.Set(GetCacheKey(chatId), chatConfig, TimeSpan.FromHours(2));
        }

        return chatConfig;
    }

    public async Task Update(ChatConfiguration chatConfiguration)
    {
        await _repository.Update(chatConfiguration, v => v.ChatId == chatConfiguration.ChatId);
        _memoryCache.Set(GetCacheKey(chatConfiguration.ChatId), chatConfiguration, TimeSpan.FromHours(2));
    }

    private static string GetCacheKey(long chatId) => $"{nameof(ChatConfiguration)}_{chatId}";
}