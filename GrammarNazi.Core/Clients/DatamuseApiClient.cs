using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.DatamuseAPI;
using GrammarNazi.Domain.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace GrammarNazi.Core.Clients;

public class DatamuseApiClient : IDatamuseApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DatamuseApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WordCheckResult> CheckWord(string word, string language)
    {
        string languageParam = "";

        if (language == SupportedLanguages.Spanish.GetLanguageInformation().TwoLetterISOLanguageName)
        {
            languageParam = "&v=es";
        }

        var httpClient = _httpClientFactory.CreateClient("datamuseApi");

        var request = new HttpRequestMessage(HttpMethod.Get, $"words?sp={HttpUtility.UrlEncode(word)}{languageParam}");

        var response = await httpClient.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();

        var result = JsonConvert.DeserializeObject<IEnumerable<WordSimilarity>>(content);

        return new()
        {
            Word = word,
            SimilarWords = result
        };
    }
}