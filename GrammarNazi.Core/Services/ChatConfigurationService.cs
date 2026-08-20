using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;

namespace GrammarNazi.Core.Services;

public class ChatConfigurationService(IRepository<ChatConfiguration> repository) : IChatConfigurationService
{
    public async Task AddConfiguration(ChatConfiguration chatConfiguration)
    {
        if (await repository.Any(x => x.ChatId == chatConfiguration.ChatId))
        {
            await Update(chatConfiguration);
            return;
        }

        await repository.Add(chatConfiguration);
    }

    public Task Delete(ChatConfiguration chatConfiguration)
    {
        return repository.Delete(chatConfiguration);
    }

    public Task<ChatConfiguration> GetConfigurationByChatId(long chatId)
    {
        return repository.GetFirst(v => v.ChatId == chatId);
    }

    public async Task Update(ChatConfiguration chatConfiguration)
    {
        await repository.Update(chatConfiguration, v => v.ChatId == chatConfiguration.ChatId);
    }
}
