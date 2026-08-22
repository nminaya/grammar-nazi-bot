using GrammarNazi.Core.Clients;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Exceptions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using Xunit;

namespace GrammarNazi.Tests.Clients;

public class CerebrasApiClientTests
{
    [Fact]
    public async Task GetChatCompletion_ServiceUnavailableFirstAttempt_RetriesAndReturnsParsedContent()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<CerebrasApiSettings>>();

        optionsMock.Value.Returns(new CerebrasApiSettings
        {
            Model = "test-model",
            ApiKey = "test-key"
        });

        int callCount = 0;
        var innerHandler = new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    Content = new StringContent("{\"error\":{\"message\":\"Service Unavailable\"}}")
                };
            }

            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"Hello from retry\"}}]}\n")
            };
        });

        var pipeline = ServiceCollectionExtensions.CreateApiResiliencePipeline(2);
        var limiter = new ServiceCollectionExtensions.SlidingWindowRateLimiter(25, TimeSpan.FromMinutes(1));
        var resilienceHandler = new ServiceCollectionExtensions.ApiResilienceHandler(limiter, pipeline)
        {
            InnerHandler = innerHandler
        };

        var httpClient = new HttpClient(resilienceHandler)
        {
            BaseAddress = new Uri("https://api.cerebras.ai/")
        };

        httpClientFactoryMock.CreateClient("cerebrasApi").Returns(httpClient);

        var client = new CerebrasApiClient(httpClientFactoryMock, optionsMock);

        // Act
        var result = await client.GetChatCompletion("system", "user");

        // Assert
        Assert.Equal("Hello from retry", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetChatCompletion_RateLimitResponseWithDeltaHeader_ThrowsExternalApiRateLimitExceptionWithRetryAfter()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<CerebrasApiSettings>>();

        optionsMock.Value.Returns(new CerebrasApiSettings
        {
            Model = "test-model",
            ApiKey = "test-key"
        });

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("{\"error\":{\"message\":\"Rate limit reached\"}}")
            };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        }))
        {
            BaseAddress = new Uri("https://api.cerebras.ai/")
        };

        httpClientFactoryMock.CreateClient("cerebrasApi").Returns(httpClient);

        var client = new CerebrasApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApiRateLimitException>(() => client.GetChatCompletion("system", "user"));
        Assert.NotNull(ex.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(30), ex.RetryAfter.Value);
    }

    [Fact]
    public async Task GetChatCompletion_RateLimitResponseWithDateHeader_ThrowsExternalApiRateLimitExceptionWithRetryAfter()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<CerebrasApiSettings>>();

        optionsMock.Value.Returns(new CerebrasApiSettings
        {
            Model = "test-model",
            ApiKey = "test-key"
        });

        var futureDate = DateTimeOffset.UtcNow.AddMinutes(2);

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("{\"error\":{\"message\":\"Rate limit reached\"}}")
            };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(futureDate);
            return response;
        }))
        {
            BaseAddress = new Uri("https://api.cerebras.ai/")
        };

        httpClientFactoryMock.CreateClient("cerebrasApi").Returns(httpClient);

        var client = new CerebrasApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApiRateLimitException>(() => client.GetChatCompletion("system", "user"));
        Assert.NotNull(ex.RetryAfter);
        Assert.True(ex.RetryAfter.Value > TimeSpan.Zero);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetChatCompletion_PermanentFailureResponse_ThrowsExternalApiPermanentFailureException(HttpStatusCode httpStatusCode)
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<CerebrasApiSettings>>();

        const string configuredModel = "test-model";
        optionsMock.Value.Returns(new CerebrasApiSettings
        {
            Model = configuredModel,
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
            BaseAddress = new Uri("https://api.cerebras.ai/")
        };

        httpClientFactoryMock.CreateClient("cerebrasApi").Returns(httpClient);

        var client = new CerebrasApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalApiPermanentFailureException>(() => client.GetChatCompletion("system", "user"));
        Assert.Contains(configuredModel, exception.Message);
        Assert.Contains("retired", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    public async Task GetChatCompletion_TransientErrorResponse_ThrowsExternalApiUnavailableException(HttpStatusCode httpStatusCode)
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<CerebrasApiSettings>>();

        optionsMock.Value.Returns(new CerebrasApiSettings
        {
            Model = "test-model",
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
            BaseAddress = new Uri("https://api.cerebras.ai/")
        };

        httpClientFactoryMock.CreateClient("cerebrasApi").Returns(httpClient);

        var client = new CerebrasApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        await Assert.ThrowsAsync<ExternalApiUnavailableException>(() => client.GetChatCompletion("system", "user"));
    }

    [Theory]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData((HttpStatusCode)418)]
    public async Task GetChatCompletion_OtherErrorResponse_ThrowsInvalidOperationException(HttpStatusCode httpStatusCode)
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<CerebrasApiSettings>>();

        optionsMock.Value.Returns(new CerebrasApiSettings
        {
            Model = "test-model",
            ApiKey = "test-key"
        });

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
                Content = new StringContent("Unclassified Error")
            };
        }))
        {
            BaseAddress = new Uri("https://api.cerebras.ai/")
        };

        httpClientFactoryMock.CreateClient("cerebrasApi").Returns(httpClient);

        var client = new CerebrasApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetChatCompletion("system", "user"));
        Assert.Contains($"Unsuccessful Cerebras API response {httpStatusCode}", exception.Message);
    }

    [Fact]
    public async Task GetChatCompletion_BadRequestWithPermanentKeyword_ThrowsExternalApiPermanentFailureException()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<CerebrasApiSettings>>();

        const string configuredModel = "test-model";
        optionsMock.Value.Returns(new CerebrasApiSettings
        {
            Model = configuredModel,
            ApiKey = "test-key"
        });

        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":{\"code\":\"model_not_found\"}}")
            };
        }))
        {
            BaseAddress = new Uri("https://api.cerebras.ai/")
        };

        httpClientFactoryMock.CreateClient("cerebrasApi").Returns(httpClient);

        var client = new CerebrasApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalApiPermanentFailureException>(() => client.GetChatCompletion("system", "user"));
        Assert.Contains(configuredModel, exception.Message);
    }

    [Fact]
    public async Task GetChatCompletion_BadRequestWithoutPermanentKeyword_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var optionsMock = Substitute.For<IOptions<CerebrasApiSettings>>();

        optionsMock.Value.Returns(new CerebrasApiSettings
        {
            Model = "test-model",
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
            BaseAddress = new Uri("https://api.cerebras.ai/")
        };

        httpClientFactoryMock.CreateClient("cerebrasApi").Returns(httpClient);

        var client = new CerebrasApiClient(httpClientFactoryMock, optionsMock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetChatCompletion("system", "user"));
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
