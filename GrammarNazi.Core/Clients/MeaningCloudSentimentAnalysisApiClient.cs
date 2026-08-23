using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.MeaningCloudAPI;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Web;

namespace GrammarNazi.Core.Clients;

public class MeaningCloudSentimentAnalysisApiClient(IHttpClientFactory httpClientFactory, IOptions<MeaningCloudSettings> options) : IMeaningCloudSentimentAnalysisApiClient
{
    private readonly MeaningCloudSettings _meaningCloudSettings = options.Value;

    public async Task<SentimentAnalysisResult> GetSentimentResult(string text, string language)
    {
        var httpClient = httpClientFactory.CreateClient("meaninCloudSentimentAnalysisApi");
        var request = new HttpRequestMessage(HttpMethod.Get, $"?key={_meaningCloudSettings.Key}&txt={HttpUtility.UrlEncode(text)}&lang={language}");

        var response = await httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<SentimentAnalysisResult>(responseJson);
    }
}
