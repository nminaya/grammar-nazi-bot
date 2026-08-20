using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.LanguageToolAPI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Web;

namespace GrammarNazi.Core.Clients;

public class LanguageToolApiClient(IHttpClientFactory httpClientFactory, ILogger<LanguageToolApiClient> logger) : ILanguageToolApiClient
{
    public async Task<LanguageToolCheckResult> Check(string text, string languageCode)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("languageToolApi");

            var request = new HttpRequestMessage(HttpMethod.Post, $"api/v2/check?text={HttpUtility.UrlEncode(text)}&language={languageCode}");

            var response = await httpClient.SendAsync(request);

            return JsonConvert.DeserializeObject<LanguageToolCheckResult>(await response.Content.ReadAsStringAsync());
        }
        catch (JsonReaderException ex)
        {
            logger.LogWarning(ex, ex.ToString());

            // return empty result
            return new()
            {
                Matches = [],
                Language = new() { Code = Defaults.LanguageCode }
            };
        }
    }
}
