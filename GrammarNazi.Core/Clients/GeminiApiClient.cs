using GrammarNazi.Domain.Entities.GeminiAPI;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Clients;

public class GeminiApiClient(IHttpClientFactory httpClientFactory)
{
    public async Task<GenerateContentResponse> GenerateContent(string promt)
    {
        // TODO: Get API from config
        var gemeniApiKey = "";

        var httpClient = httpClientFactory.CreateClient("geminiApi");

        var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/gemini-1.5-flash:generateContent?key={gemeniApiKey}")
        {
            Content = JsonContent.Create(GenerateContentRequest.CreateRequestObject(promt))
        };

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Unsuccessful Gemini API response {response.StatusCode}", new(await response.Content.ReadAsStringAsync()));
        }

        var content = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<GenerateContentResponse>(content);
    }
}
