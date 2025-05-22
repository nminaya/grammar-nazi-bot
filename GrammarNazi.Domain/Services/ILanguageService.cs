using GrammarNazi.Domain.Entities;

namespace GrammarNazi.Domain.Services;

public interface ILanguageService
{
    Task<LanguageInformation> IdentifyLanguage(string text);
}
