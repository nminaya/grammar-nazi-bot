using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Entities.LanguageToolAPI;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class LanguageToolApiService : BaseGrammarService, IGrammarService
    {
        private readonly ILanguageToolApiClient _apiClient;
        private readonly ILanguageService _languageService;

        public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.LanguageToolApi;

        public LanguageToolApiService(ILanguageToolApiClient apiClient, ILanguageService languageService)
        {
            _apiClient = apiClient;
            _languageService = languageService;
        }

        public async Task<GrammarCheckResult> GetCorrections(string text)
        {
            // Do not evalulate long texts or empty texts
            if (string.IsNullOrWhiteSpace(text) || text.Length >= Defaults.LanguageToolApiMaxTextLength)
            {
                return new(default);
            }

            string languageCode;
            if (SelectedLanguage != SupportedLanguages.Auto)
            {
                languageCode = SelectedLanguage.GetLanguageInformation().TwoLetterISOLanguageName;
            }
            else
            {
                var languageInfo = _languageService.IdentifyLanguage(text);

                if (languageInfo == default)
                {
                    // language not supported
                    return new(default);
                }

                // Use API language auto detection
                languageCode = "auto";
            }

            var result = await _apiClient.Check(text, languageCode);

            // validate if LanguageTool has detected a valid language
            if (!IsValidLanguageDetected(result.Language.Code))
                return new(default);

            var corrections = new List<GrammarCorrection>();

            var matches = result
                            .Matches.Where(RulesFilter)
                            .Where(v => v.Replacements.Count > 0);

            foreach (var match in matches)
            {
                var wrongWord = match.Context.Text.Substring(match.Context.Offset, match.Context.Length);

                if (IsWhiteListWord(wrongWord))
                    continue;

                var correction = new GrammarCorrection
                {
                    WrongWord = wrongWord,
                    PossibleReplacements = match.Replacements.Select(v => v.Value),
                    Message = match.Message
                };

                corrections.Add(correction);
            }

            return new(corrections);
        }

        private bool RulesFilter(Match match)
        {
            if (SelectedStrictnessLevel == CorrectionStrictnessLevels.Intolerant)
                return true;

            // TODO: Create a list of default or disabled rules
            // TODO: Use the API endpoint parameter "disabledRules"

            // Do not get the following matching rules
            return !match.Rule.Id.Contains("PUNCTUATION")
                && !match.Rule.Id.Contains("WHITESPACE")
                && match.Rule.Id != "UPPERCASE_SENTENCE_START"
                && match.Rule.Id != "PROFANITY"
                && match.Rule.Id != "MORFOLOGIK_RULE_ES"
                && match.Rule.Id != "MORFOLOGIK_RULE_EN_US"
                && match.Rule.Id != "EN_QUOTES"
                && match.Rule.Id != "SPANISH_WORD_REPEAT_RULE"
                && match.Rule.Id != "ES_QUESTION_MARK"
                && match.Rule.Id != "GONNA"
                && match.Rule.Id != "DECIMAL_COMMA"
                && match.Rule.Id != "ONOMATOPEYAS"
                && match.Rule.Id != "INCORRECT_SPACES";
        }

        private bool IsValidLanguageDetected(string languageCode)
        {
            return LanguageUtils.GetSupportedLanguages()
                .Select(LanguageUtils.GetLanguageCode)
                .Contains(languageCode[..2]);
        }
    }
}