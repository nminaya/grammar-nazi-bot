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

public class GeminiApiClientTests
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GenerateContent_PermanentFailureResponse_ThrowsExternalApiPermanentFailureException(HttpStatusCode httpStatusCode)
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<GeminiApiSettings>>();

        const string configuredModel = "test-model-version";
        optionsMock.Value.Returns(new GeminiApiSettings
        {
            ModelVersion = configuredModel,
            ApiKey = "test-key"
        });

        var contentStr = httpStatusCode == HttpStatusCode.NotFound
            ? "{\"message\":\"Model does not exist or you do not have access to it.\",\"type\":\"not_found_error\",\"param\":\"model\",\"code\":\"model_not_found\"}"
            : "Error Content";

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
                Content = new StringContent(contentStr)
            };
        }))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        httpClientFactoryMock.CreateClient("geminiApi").Returns(httpClient);

        var client = new GeminiApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalApiPermanentFailureException>(() => client.GenerateContent("prompt"));
        Assert.Contains(configuredModel, exception.Message);
        Assert.Contains("retired", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task GenerateContent_TransientErrorResponse_ThrowsExternalApiUnavailableException(HttpStatusCode httpStatusCode)
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<GeminiApiSettings>>();

        optionsMock.Value.Returns(new GeminiApiSettings
        {
            ModelVersion = "test-model-version",
            ApiKey = "test-key"
        });

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
                Content = new StringContent("{\"error\":{\"message\":\"Transient error\"}}")
            };
        }))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        httpClientFactoryMock.CreateClient("geminiApi").Returns(httpClient);

        var client = new GeminiApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        await Assert.ThrowsAsync<ExternalApiUnavailableException>(() => client.GenerateContent("prompt"));
    }

    [Fact]
    public async Task GenerateContent_OtherErrorResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<GeminiApiSettings>>();

        optionsMock.Value.Returns(new GeminiApiSettings
        {
            ModelVersion = "test-model-version",
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
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        httpClientFactoryMock.CreateClient("geminiApi").Returns(httpClient);

        var client = new GeminiApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GenerateContent("prompt"));
        Assert.Contains("Unsuccessful Gemini API response InternalServerError", exception.Message);
    }

    [Fact]
    public async Task GenerateContent_BadRequestWithApiKeyInvalid_ThrowsExternalApiPermanentFailureException()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<GeminiApiSettings>>();

        const string configuredModel = "test-model-version";
        optionsMock.Value.Returns(new GeminiApiSettings
        {
            ModelVersion = configuredModel,
            ApiKey = "test-key"
        });

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":{\"message\":\"API_KEY_INVALID\"}}")
            };
        }))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        httpClientFactoryMock.CreateClient("geminiApi").Returns(httpClient);

        var client = new GeminiApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalApiPermanentFailureException>(() => client.GenerateContent("prompt"));
        Assert.Contains(configuredModel, exception.Message);
    }

    [Fact]
    public async Task GenerateContent_BadRequestWithoutPermanentKeyword_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<GeminiApiSettings>>();

        optionsMock.Value.Returns(new GeminiApiSettings
        {
            ModelVersion = "test-model-version",
            ApiKey = "test-key"
        });

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Generic error")
            };
        }))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        httpClientFactoryMock.CreateClient("geminiApi").Returns(httpClient);

        var client = new GeminiApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GenerateContent("prompt"));
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
