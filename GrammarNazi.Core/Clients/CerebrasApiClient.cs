using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

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
            throw new InvalidOperationException($"Unsuccessful Cerebras API response {response.StatusCode}", new Exception(errorContent));
        }

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CebrasChatCompletionResponse>(content);

        return result?.Choices?[0]?.Message?.Content ?? string.Empty;
    }

    private class CebrasChatCompletionResponse
    {
        public List<Choice> Choices { get; set; }

        public class Choice
        {
            public Message Message { get; set; }
        }

        public class Message
        {
            public string Content { get; set; }
        }
    }
}
