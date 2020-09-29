using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class YandexSpellerApiService : IGrammarService
    {
        private readonly IYandexSpellerApiClient _yandexSpellerApiClient;
        private readonly ILanguageService _languageService;

        private SupportedLanguages _selectedLanguage;

        public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.YandexSpellerApi;

        public YandexSpellerApiService(IYandexSpellerApiClient yandexSpellerApiClient, ILanguageService languageService)
        {
            _yandexSpellerApiClient = yandexSpellerApiClient;
            _languageService = languageService;
        }

        public async Task<GrammarCheckResult> GetCorrections(string text)
        {
            string languageCode;
            if (_selectedLanguage != SupportedLanguages.Auto)
            {
                languageCode = LanguageUtils.GetLanguageCode(_selectedLanguage.GetDescription());
            }
            else
            {
                var languageInfo = _languageService.IdentifyLanguage(text);

                if (languageInfo == default)
                {
                    return new GrammarCheckResult(default);
                }

                languageCode = languageInfo.TwoLetterISOLanguageName;
            }

            var response = await _yandexSpellerApiClient.CheckText(text, languageCode);

            if (response.Any())
            {
                var corrections = new List<GrammarCorrection>();

                foreach (var spellResult in response)
                {
                    var correction = new GrammarCorrection
                    {
                        WrongWord = spellResult.Word,
                        PossibleReplacements = spellResult.S,
                        Message = $"The word \"{spellResult.Word}\" doesn't exist or isn't in the dictionary."
                    };

                    corrections.Add(correction);
                }

                return new GrammarCheckResult(corrections);
            }

            return new GrammarCheckResult(default);
        }

        public void SetSelectedLanguage(SupportedLanguages supportedLanguage)
        {
            _selectedLanguage = supportedLanguage;
        }
    }
}