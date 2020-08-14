using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class LanguageToolApiService : IGrammarService
    {
        private readonly ILanguageToolApiClient _apiClient;

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

            // TODO: Create a list of default rules
            // Do not get punctuation or uppercase corrections 
            var matches = result.Matches.Where(v => !v.Rule.Id.Contains("PUNCTUATION") && !v.Rule.Id.Contains("WHITESPACE") && v.Rule.Id != "UPPERCASE_SENTENCE_START");

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
    }
}