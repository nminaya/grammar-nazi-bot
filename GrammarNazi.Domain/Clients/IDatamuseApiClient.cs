using GrammarNazi.Domain.Entities.DatamuseAPI;

namespace GrammarNazi.Domain.Clients;

public interface IDatamuseApiClient
{
    Task<WordCheckResult> CheckWord(string word, string language);
}