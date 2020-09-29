using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using YandexSpeller;

namespace GrammarNazi.Core.Services
{
    public class YandexSpellerApiService : IGrammarService
    {
        private readonly SpellServiceSoap _spellServiceSoap;
        private readonly ILanguageService _languageService;

        private SupportedLanguages _selectedLanguage;

        public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.YandexSpellerApi;

        public YandexSpellerApiService(SpellServiceSoap spellServiceSoap, ILanguageService languageService)
        {
            _spellServiceSoap = spellServiceSoap;
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

            var checkTextRequest = new checkTextRequest1
            {
                CheckTextRequest = new CheckTextRequest
                {
                    text = text,
                    lang = languageCode
                }
            };

            var response = await _spellServiceSoap.checkTextAsync(checkTextRequest);

            if (response.CheckTextResponse.SpellResult.Length != 0)
            {
                var corrections = new List<GrammarCorrection>();

                foreach (var spellResult in response.CheckTextResponse.SpellResult)
                {
                    var correction = new GrammarCorrection
                    {
                        WrongWord = spellResult.word,
                        PossibleReplacements = spellResult.s,
                        Message = $"The word \"{spellResult.word}\" doesn't exist or isn't in the dictionary."
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