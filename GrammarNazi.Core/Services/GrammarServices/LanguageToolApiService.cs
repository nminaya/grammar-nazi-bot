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

namespace GrammarNazi.Core.Services;

public class LanguageToolApiService : BaseGrammarService, IGrammarService
{
    private readonly ILanguageToolApiClient _apiClient;
    private readonly ILanguageService _languageService;

    public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.LanguageToolApi;

    private static readonly List<string> DefaultDisabledRules = new()
    {
        "UPPERCASE_SENTENCE_START",
        "PROFANITY",
        "MORFOLOGIK_RULE_ES",
        "MORFOLOGIK_RULE_EN_US",
        "EN_QUOTES",
        "SPANISH_WORD_REPEAT_RULE",
        "ES_QUESTION_MARK",
        "GONNA",
        "DECIMAL_COMMA",
        "ONOMATOPEYAS",
        "INCORRECT_SPACES"
    };

    public LanguageToolApiService(ILanguageToolApiClient apiClient, ILanguageService languageService)
    {
        _apiClient = apiClient;
        _languageService = languageService;
    }

    public async Task<GrammarCheckResult> GetCorrections(string text)
    {
        // Do not evaluate long texts or empty texts
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
            var languageInfo = await _languageService.IdentifyLanguage(text);

            if (languageInfo == default)
            {
                // language not supported
                return new(default);
            }

            // Use API language auto detection
            languageCode = "auto";
        }

        string rulesToDisable = null;
        if (SelectedStrictnessLevel != CorrectionStrictnessLevels.Intolerant)
        {
            rulesToDisable = string.Join(",", DefaultDisabledRules);
        }

        var result = await _apiClient.Check(text, languageCode, rulesToDisable);

        // validate if LanguageTool has detected a valid language
        if (!IsValidLanguageDetected(result?.Language?.Code))
        {
            return new(default);
        }

        var corrections = new List<GrammarCorrection>();

        // Server-side filtering is now used, so we only filter out matches with no replacements.
        // The RulesFilter method is removed.
        var matches = result
                        .Matches.Where(v => v.Replacements.Count > 0);

        foreach (var match in matches)
        {
            var wrongWord = match.Context.Text.Substring(match.Context.Offset, match.Context.Length);

            if (IsWhiteListWord(wrongWord))
            {
                continue;
            }

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

    private bool IsValidLanguageDetected(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            return false;
        }

        return LanguageUtils.GetSupportedLanguages()
            .Select(LanguageUtils.GetLanguageCode)
            .Contains(languageCode[..2]);
    }
}