using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.SentimAPI;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace GrammarNazi.Core.Clients;

public class SentimApiClient(IHttpClientFactory httpClientFactory) : ISentimApiClient
{
    public async Task<SentimResult> GetSentimentResult(string text)
    {
        var httpClient = httpClientFactory.CreateClient("sentimApi");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/")
        {
            Content = JsonContent.Create(new SentimRequest(text))
        };
        var response = await httpClient.SendAsync(request);

        return JsonConvert.DeserializeObject<SentimResult>(await response.Content.ReadAsStringAsync());
    }
}
