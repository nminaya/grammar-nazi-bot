using GrammarNazi.Domain.Entities.MeaningCloudAPI;

namespace GrammarNazi.Domain.Clients;

public interface IMeganingCloudLangApiClient
{
    Task<LanguageDetectionResult> GetLanguage(string text);
}
