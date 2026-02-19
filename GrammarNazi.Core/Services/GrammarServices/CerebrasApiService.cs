using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services.GrammarServices;

public class CerebrasApiService(ICerebrasApiClient cerebrasApiClient) : BaseGrammarService, IGrammarService
{
    public GrammarAlgorithms GrammarAlgorithm => GrammarAlgorithms.CerebrasApi;

    public async Task<GrammarCheckResult> GetCorrections(string text)
    {
        var systemPrompt = GetSystemPrompt();
        var userPrompt = $"Check this text: {text}";

        var resultJsonString = await cerebrasApiClient.GetChatCompletion(systemPrompt, userPrompt);

        if (string.IsNullOrEmpty(resultJsonString))
        {
            return new(default);
        }

        var cleanedJson = resultJsonString.Replace("```json", "").Replace("```", "").Trim();

        try
        {
            var grammarCorrections = JsonConvert.DeserializeObject<IEnumerable<GrammarCorrection>>(cleanedJson);

            if (grammarCorrections == null || !grammarCorrections.Any())
            {
                return new(default);
            }

            return new(grammarCorrections.Where(x => !IsWhiteListWord(x.WrongWord) && x.PossibleReplacements?.Any() == true));
        }
        catch (JsonException)
        {
            // Failed to parse response
            return new(default);
        }
    }

    private string GetSystemPrompt()
    {
        var languageSection = SelectedLanguage == SupportedLanguages.Auto
            ? "Auto detect the language"
            : $"The language is {SelectedLanguage.GetDescription()}";

        return @$"You are a grammar checker. Analyze the text for any grammar, spelling, or orthographic errors. For each mistake, provide the result in the JSON format below. {languageSection}.
Build the results in that same language. Only provide a RFC8259 compliant JSON response without deviation. Do not include any explanation, only the JSON array.

[{{
  ""wrongWord"": """",
  ""message"": """",
  ""possibleReplacements"": [""""]
}}]

If there are no errors, return an empty array: []";
    }
}
