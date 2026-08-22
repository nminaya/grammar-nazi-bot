using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.OpenAiAPI;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;

namespace GrammarNazi.Core.Clients;

public class CerebrasApiClient(IHttpClientFactory httpClientFactory, IOptions<CerebrasApiSettings> options) : ICerebrasApiClient
{
    private readonly CerebrasApiSettings _cerebrasApiSettings = options.Value;

    public async Task<string> GetChatCompletion(string systemPrompt, string userPrompt)
    {
        if (string.IsNullOrEmpty(_cerebrasApiSettings.Model))
        {
            throw new InvalidOperationException("Cerebras Model is not configured");
        }

        if (string.IsNullOrEmpty(_cerebrasApiSettings.ApiKey))
        {
            throw new InvalidOperationException("Cerebras ApiKey is not configured");
        }

        var httpClient = httpClientFactory.CreateClient("cerebrasApi");

        var requestBody = new
        {
            model = _cerebrasApiSettings.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.1,
            max_tokens = 1024
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
        {
            Content = JsonContent.Create(requestBody)
        };

        request.Headers.Add("Authorization", $"Bearer {_cerebrasApiSettings.ApiKey}");

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw ExternalApiResponseHelper.CreateExceptionForErrorResponse(response, "Cerebras", _cerebrasApiSettings.Model, errorContent);
        }

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<OpenAiChatCompletionResponse>(content);

        return result?.Choices?[0]?.Message?.Content ?? string.Empty;
    }
}
