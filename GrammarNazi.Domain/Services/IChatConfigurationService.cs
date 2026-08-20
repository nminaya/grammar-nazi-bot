using GrammarNazi.Domain.Entities;

namespace GrammarNazi.Domain.Services;

public interface IChatConfigurationService
{
    Task AddConfiguration(ChatConfiguration chatConfiguration);

    Task Delete(ChatConfiguration chatConfiguration);

    Task Update(ChatConfiguration chatConfiguration);

    Task<ChatConfiguration> GetConfigurationByChatId(long chatId);
}
