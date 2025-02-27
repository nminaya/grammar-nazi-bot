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

public class GeminiApiService(IGeminiApiClient geminiApiClient) : BaseGrammarService, IGrammarService
{
    public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.Gemini;

    public async Task<GrammarCheckResult> GetCorrections(string text)
    {
        var prompt = GetCorrectionPrompt(text);

        var result = await geminiApiClient.GenerateContent(prompt);

        if (result.Candidates.Count == 0)
        {
            // Empty result received
            return new(default);
        }

        var resultJsonString = result.Candidates.First().Content.Parts.FirstOrDefault()?.Text;

        if (string.IsNullOrEmpty(resultJsonString))
        {
            // Empty result received
            return new(default);
        }

        var cleanedJson = resultJsonString.Replace("```json", "").Replace("```", "").Trim();

        var grammarCorrections = JsonConvert.DeserializeObject<IEnumerable<GrammarCorrection>>(cleanedJson);

        if (!grammarCorrections.Any())
        {
            // No corrections
            return new(default);
        }

        return new(grammarCorrections);
    }

    private string GetCorrectionPrompt(string text)
    {
        var languageSection = SelectedLanguage == SupportedLanguages.Auto
            ? "Auto detect the language"
            : $"The language is {SelectedLanguage.GetDescription()}";

        return @$"Give me grammar or orthographic corrections of the text below and reply using the below json format. {languageSection}. 
                Give me the message in that same language. If the word doesn't have possibleReplacements, don't add it to the results.

        [{{    

          ""wrongWord"": """",    

          ""message"": """",    

          ""possibleReplacements"": [""""]    

        }}]    

        Text: {text}";
    }
}
