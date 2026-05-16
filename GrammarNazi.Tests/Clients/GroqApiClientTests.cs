using GrammarNazi.Core.Clients;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Exceptions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.Clients;

public class GroqApiClientTests
{
    [Fact]
    public async Task GetChatCompletion_RateLimitResponse_ThrowsGroqRateLimitException()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<GroqApiSettings>>();

        optionsMock.Value.Returns(new GroqApiSettings
        {
            Model = "test-model",
            ApiKey = "test-key"
        });

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("{\"error\":{\"message\":\"Rate limit reached\"}}")
            };
        }))
        {
            BaseAddress = new Uri("https://api.groq.com/")
        };

        httpClientFactoryMock.CreateClient("groqApi").Returns(httpClient);

        var client = new GroqApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        await Assert.ThrowsAsync<GroqRateLimitException>(() => client.GetChatCompletion("system", "user"));
    }

    [Fact]
    public async Task GetChatCompletion_OtherErrorResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<GroqApiSettings>>();

        optionsMock.Value.Returns(new GroqApiSettings
        {
            Model = "test-model",
            ApiKey = "test-key"
        });

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error")
            };
        }))
        {
            BaseAddress = new Uri("https://api.groq.com/")
        };

        httpClientFactoryMock.CreateClient("groqApi").Returns(httpClient);

        var client = new GroqApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetChatCompletion("system", "user"));
        Assert.Contains("Unsuccessful Groq API response InternalServerError", exception.Message);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _sendAsync(request, cancellationToken);
        }
    }
}
