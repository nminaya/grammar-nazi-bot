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
    private readonly GeminiApiSettings _geminiApiSettings = options.Value;

    public async Task<GenerateContentResponse> GenerateContent(string promt)
    {
        if (string.IsNullOrEmpty(_geminiApiSettings.ModelVersion))
        {
            throw new InvalidOperationException("ModelVersion is not configured");
        }

        var httpClient = httpClientFactory.CreateClient("geminiApi");

        var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{_geminiApiSettings.ModelVersion}:generateContent?key={_geminiApiSettings.ApiKey}")
        {
            Content = JsonContent.Create(GenerateContentRequest.CreateRequestObject(promt))
        };

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable
                || response.StatusCode == HttpStatusCode.BadGateway
                || response.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                throw new ExternalApiUnavailableException($"Gemini API is currently unavailable ({response.StatusCode}).", new Exception(errorContent));
            }

            throw new InvalidOperationException($"Unsuccessful Gemini API response {response.StatusCode}", new Exception(errorContent));
        }

        var content = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<GenerateContentResponse>(content);
    }
}
