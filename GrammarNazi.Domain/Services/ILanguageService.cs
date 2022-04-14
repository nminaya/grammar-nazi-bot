using GrammarNazi.Domain.Entities;

namespace GrammarNazi.Domain.Services;

public interface ILanguageService
{
    LanguageInformation IdentifyLanguage(string text);
}
