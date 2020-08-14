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

        public async Task<CheckResult> GetCorrections(string text)
        {
            if (text.Length > 50_000)
            {
                return new CheckResult();
            }

            var result = await _apiClient.Check(text);

            var resultErrors = new List<ResultError>();

            // TODO: Refactor
            // Do not get punctuation or uppercase corrections 
            var matches = result.Matches.Where(v => v.Rule.Id != "PUNCTUATION_PARAGRAPH_END" || v.Rule.Id != "UPPERCASE_SENTENCE_START");

            foreach (var item in matches)
            {
                var resultError = new ResultError
                {
                    WrongWord = item.Context.Text.Substring(item.Context.Offset, item.Context.Length),
                    PossibleReplacements = item.Replacements.Select(v => v.Value)
                };

                resultErrors.Add(resultError);
            }

            return new CheckResult
            {
                HasErrors = result.Matches.Any(),
                ResultErrors = resultErrors
            };
        }
    }
}