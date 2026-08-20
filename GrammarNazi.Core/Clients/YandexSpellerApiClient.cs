using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.YandexSpellerAPI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Web;

namespace GrammarNazi.Core.Clients;

public class YandexSpellerApiClient(IHttpClientFactory httpClientFactory, ILogger<YandexSpellerApiClient> logger) : IYandexSpellerApiClient
{
    public async Task<IEnumerable<CheckTextResponse>> CheckText(string text, string language)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("yandexSpellerApi");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/services/spellservice.json/checkText?text={HttpUtility.UrlEncode(text)}&lang={language}");

            var response = await httpClient.SendAsync(request);
            var jsonString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<IEnumerable<CheckTextResponse>>(jsonString);
        }
        catch (JsonReaderException ex)
        {
            logger.LogWarning(ex, ex.ToString());

            // return empty result
            return Enumerable.Empty<CheckTextResponse>();
        }
    }
}
