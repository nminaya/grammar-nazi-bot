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

public class GroqApiClient(IHttpClientFactory httpClientFactory, IOptions<GroqApiSettings> options) : IGroqApiClient
{
    private readonly GroqApiSettings _groqApiSettings = options.Value;

    public async Task<string> GetChatCompletion(string systemPrompt, string userPrompt)
    {
        if (string.IsNullOrEmpty(_groqApiSettings.Model))
        {
            throw new InvalidOperationException("Groq Model is not configured");
        }

        if (string.IsNullOrEmpty(_groqApiSettings.ApiKey))
        {
            throw new InvalidOperationException("Groq ApiKey is not configured");
        }

        var httpClient = httpClientFactory.CreateClient("groqApi");

        var requestBody = new
        {
            model = _groqApiSettings.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.1,
            max_tokens = 1024
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "openai/v1/chat/completions")
        {
            Content = JsonContent.Create(requestBody)
        };

        request.Headers.Add("Authorization", $"Bearer {_groqApiSettings.ApiKey}");

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Unsuccessful Groq API response {response.StatusCode}", new Exception(errorContent));
        }

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GroqChatCompletionResponse>(content);

        return result?.Choices?[0]?.Message?.Content ?? string.Empty;
    }

    private class GroqChatCompletionResponse
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
