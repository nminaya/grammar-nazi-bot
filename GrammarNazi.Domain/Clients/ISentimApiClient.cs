using GrammarNazi.Domain.Entities.SentimAPI;

namespace GrammarNazi.Domain.Clients;

public interface ISentimApiClient
{
    Task<SentimResult> GetSentimentResult(string text);
}