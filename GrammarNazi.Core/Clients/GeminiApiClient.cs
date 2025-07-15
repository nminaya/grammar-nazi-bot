using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.GeminiAPI;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Clients;

public class GeminiApiClient(IHttpClientFactory httpClientFactory, IOptions<GeminiApiSettings> options) : IGeminiApiClient
{
    private readonly GeminiApiSettings _geminiApiSettings = options.Value;

    public async Task<GenerateContentResponse> GenerateContent(string promt)
    {
        var httpClient = httpClientFactory.CreateClient("geminiApi");

        var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{_geminiApiSettings.ModelVersion}:generateContent?key={_geminiApiSettings.ApiKey}")
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
