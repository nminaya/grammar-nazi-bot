using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Entities.LanguageToolAPI;
using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class LanguageToolApiService : IGrammarService
    {
        private readonly ILanguageToolApiClient _apiClient;
        public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.LanguageToolApi;

        public LanguageToolApiService(ILanguageToolApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<GrammarCheckResult> GetCorrections(string text)
        {
            // Do not evalulate long texts
            if (text.Length >= 10_000)
            {
                return new GrammarCheckResult(null);
            }

            var result = await _apiClient.Check(text);

            var corrections = new List<GrammarCorrection>();

            var matches = result
                            .Matches.Where(RulesFilter)
                            .Where(v => v.Replacements.Count > 0);

            foreach (var match in matches)
            {
                var correction = new GrammarCorrection
                {
                    WrongWord = match.Context.Text.Substring(match.Context.Offset, match.Context.Length),
                    PossibleReplacements = match.Replacements.Select(v => v.Value)
                };

                corrections.Add(correction);
            }

            return new GrammarCheckResult(corrections);
        }

        private bool RulesFilter(Match match)
        {
            // TODO: Create a list of default rules
            // Do not get punctuation or uppercase corrections
            return !match.Rule.Id.Contains("PUNCTUATION")
                && !match.Rule.Id.Contains("WHITESPACE")
                && match.Rule.Id != "UPPERCASE_SENTENCE_START"
                && match.Rule.Id != "PROFANITY";
        }
    }
}