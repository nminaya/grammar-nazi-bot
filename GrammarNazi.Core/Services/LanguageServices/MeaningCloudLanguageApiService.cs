using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using System.Linq;

namespace GrammarNazi.Core.Services
{
    public class MeaningCloudLanguageApiService : ILanguageService
    {
        private readonly IMeganingCloudLangApiClient _meganingLanguageIdentificationApi;

        public MeaningCloudLanguageApiService(IMeganingCloudLangApiClient meganingLanguageIdentificationApi)
        {
            _meganingLanguageIdentificationApi = meganingLanguageIdentificationApi;
        }

        public LanguageInformation IdentifyLanguage(string text)
        {
            //TODO: Make ILanguageService.IdentifyLanguage async
            var languageResult = _meganingLanguageIdentificationApi.GetLanguage(text).GetAwaiter().GetResult();

            if (!languageResult.LanguageList.Any())
                return default;

            var languages = languageResult.LanguageList.Where(v => LanguageUtils.GetSupportedLanguages().Contains(v.Iso6393));

            if (!languages.Any())
                return default;

            var language = languages.First();

            return new LanguageInformation
            {
                ThreeLetterISOLanguageName = language.Iso6393,
                TwoLetterISOLanguageName = language.Iso6392
            };
        }
    }
}