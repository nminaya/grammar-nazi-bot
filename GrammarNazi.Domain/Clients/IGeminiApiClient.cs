using GrammarNazi.Domain.Entities.GeminiAPI;

namespace GrammarNazi.Domain.Clients;

public interface IGeminiApiClient
{
    Task<GenerateContentResponse> GenerateContent(string promt);
}
