using GrammarNazi.Domain.Entities.LanguageToolAPI;

namespace GrammarNazi.Domain.Clients;

public interface ILanguageToolApiClient
{
    Task<LanguageToolCheckResult> Check(string text, string languageCode);
}
