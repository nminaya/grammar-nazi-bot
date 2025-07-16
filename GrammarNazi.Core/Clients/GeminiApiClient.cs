using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.GeminiAPI;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Exceptions;
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
    private readonly string _apiKey = options.Value.ApiKey;

    public async Task<GenerateContentResponse> GenerateContent(string promt)
    {
        var httpClient = httpClientFactory.CreateClient("geminiApi");

        var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}")
        {
            Content = JsonContent.Create(GenerateContentRequest.CreateRequestObject(promt))
        };

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                throw new GeminiServiceUnavailableException("Gemini API is currently unavailable.");
            }

            throw new InvalidOperationException($"Unsuccessful Gemini API response {response.StatusCode}", new(await response.Content.ReadAsStringAsync()));
        }

        var content = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<GenerateContentResponse>(content);
    }
}
