using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.MeaningCloudAPI;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Web;

namespace GrammarNazi.Core.Clients;

public class MeganingCloudLangApiClient(IHttpClientFactory httpClientFactory, IOptions<MeaningCloudSettings> options) : IMeganingCloudLangApiClient
{
    private readonly MeaningCloudSettings _meaningCloudSettings = options.Value;

    public async Task<LanguageDetectionResult> GetLanguage(string text)
    {
        var httpClient = httpClientFactory.CreateClient("meaninCloudLanguageApi");
        var request = new HttpRequestMessage(HttpMethod.Get, $"?key={_meaningCloudSettings.Key}&txt={HttpUtility.UrlEncode(text)}");

        var response = await httpClient.SendAsync(request);

        return JsonConvert.DeserializeObject<LanguageDetectionResult>(await response.Content.ReadAsStringAsync());
    }
}
