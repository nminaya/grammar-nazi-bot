using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Entities.YandexSpellerAPI;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class YandexSpellerApiService : BaseGrammarService, IGrammarService
    {
        private readonly IYandexSpellerApiClient _yandexSpellerApiClient;
        private readonly ILanguageService _languageService;

        public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.YandexSpellerApi;

        public YandexSpellerApiService(IYandexSpellerApiClient yandexSpellerApiClient, ILanguageService languageService)
        {
            _yandexSpellerApiClient = yandexSpellerApiClient;
            _languageService = languageService;
        }

        public async Task<GrammarCheckResult> GetCorrections(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new GrammarCheckResult(default);

            string languageCode;
            if (SelectedLanguage != SupportedLanguages.Auto)
            {
                languageCode = LanguageUtils.GetLanguageCode(SelectedLanguage.GetDescription());
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

            if (response?.Any() == true)
            {
                var corrections = new List<GrammarCorrection>();

                foreach (var spellResult in response.Where(ErrorCodeFIlter))
                {
                    var correction = new GrammarCorrection
                    {
                        WrongWord = spellResult.Word,
                        PossibleReplacements = spellResult.Replacements,
                        Message = GetErrorMessage(spellResult)
                    };

                    corrections.Add(correction);
                }

                return new GrammarCheckResult(corrections);
            }

            return new GrammarCheckResult(default);
        }

        private string GetErrorMessage(CheckTextResponse checkTextResponse)
        {
            return checkTextResponse.ErrorCode switch
            {
                YandexSpellerErrorCodes.RepeatWord => $"Repeated word.",
                YandexSpellerErrorCodes.Capitalization => "Incorrect use of uppercase and lowercase letters.",
                _ => GetDefaultErrorMessage(),
            };

            string GetDefaultErrorMessage()
            {
                if (checkTextResponse.Word.Split(' ').Length > 1)
                    return $"Possible mistake found.";

                return $"The word \"{checkTextResponse.Word}\" doesn't exist or isn't in the dictionary.";
            }
        }

        private bool ErrorCodeFIlter(CheckTextResponse response)
        {
            if (SelectedStrictnessLevel == CorrectionStrictnessLevels.Intolerant)
                return true;

            return response.ErrorCode == YandexSpellerErrorCodes.UnknownWord;
        }
    }
}