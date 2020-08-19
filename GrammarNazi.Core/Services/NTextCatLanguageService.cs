using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using NTextCat;
using System;
using System.Globalization;
using System.Linq;

namespace GrammarNazi.Core.Services
{
    public class NTextCatLanguageService : ILanguageService
    {
        private readonly RankedLanguageIdentifierFactory _rankedLanguageIdentifierFactory;

        public NTextCatLanguageService()
        {
            _rankedLanguageIdentifierFactory = new RankedLanguageIdentifierFactory();
        }

        public LanguageInformation IdentifyLanguage(string text)
        {
            var identifier = _rankedLanguageIdentifierFactory.Load("Library/Core14.profile.xml");
            var languages = identifier.Identify(text);

            if (!languages.Any())
                return null;

            var (languageInfo, _) = languages.FirstOrDefault();

            return new LanguageInformation
            {
                ThreeLetterISOLanguageName = languageInfo.Iso639_3,
                TwoLetterISOLanguageName = GetLanguageCode(languageInfo.Iso639_3)
            };
        }

        private string GetLanguageCode(string langCode)
        {
            var culture = CultureInfo.GetCultures(CultureTypes.AllCultures)
                            .FirstOrDefault(v => v.ThreeLetterISOLanguageName == langCode || v.ThreeLetterWindowsLanguageName.ToLower() == langCode);

            return culture.TwoLetterISOLanguageName;
        }
    }
}