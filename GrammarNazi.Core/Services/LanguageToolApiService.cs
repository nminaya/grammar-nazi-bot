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
            if (string.IsNullOrWhiteSpace(text) || text.Length >= Defaults.MaxLengthText)
            {
                return new GrammarCheckResult(default);
            }

            string languageCode;
            if (SelectedLanguage != SupportedLanguages.Auto)
            {
                languageCode = LanguageUtils.GetLanguageCode(SelectedLanguage.GetDescription());
            }
            else
            {
                var languageInfo = _languageService.IdentifyLanguage(text);

                if (languageInfo != default)
                {
                    // Use API language auto detection
                    languageCode = "auto";
                }
                else // language not supported
                {
                    return new GrammarCheckResult(default);
                }
            }

            var result = await _apiClient.Check(text, languageCode);

            // validate if LanguageTool has detected a valid language
            if (!IsValidLanguageDetected(result.Language.Code))
                return new GrammarCheckResult(default);

            var corrections = new List<GrammarCorrection>();

            var matches = result
                            .Matches.Where(RulesFilter)
                            .Where(v => v.Replacements.Count > 0);

            foreach (var match in matches)
            {
                var correction = new GrammarCorrection
                {
                    WrongWord = match.Context.Text.Substring(match.Context.Offset, match.Context.Length),
                    PossibleReplacements = match.Replacements.Select(v => v.Value),
                    Message = match.Message
                };

                corrections.Add(correction);
            }

            return new GrammarCheckResult(corrections);
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
                && match.Rule.Id != "ES_QUESTION_MARK"
                && match.Rule.Id != "GONNA";
        }

        private bool IsValidLanguageDetected(string languageCode)
        {
            return LanguageUtils.GetSupportedLanguages()
                .Select(LanguageUtils.GetLanguageCode)
                .Contains(languageCode[..2]);
        }
    }
}