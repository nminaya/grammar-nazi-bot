using GrammarNazi.Core.Clients;
using NSubstitute;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace GrammarNazi.Tests.Clients;

public class DatamuseApiClientTests
{
    [Fact]
    public async Task CheckWord_HtmlResponse_ReturnsEmptySimilarWords()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("<html><body>Error</body></html>")
            };
        }))
        {
            BaseAddress = new Uri("https://api.datamuse.com/")
        };

        httpClientFactoryMock.CreateClient("datamuseApi").Returns(httpClient);

        var client = new DatamuseApiClient(httpClientFactoryMock);

        // Act
        var result = await client.CheckWord("test", "en");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Word);
        Assert.Empty(result.SimilarWords);
    }

    [Fact]
    public async Task CheckWord_NonSuccessStatusCode_ReturnsEmptySimilarWords()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error")
            };
        }))
        {
            BaseAddress = new Uri("https://api.datamuse.com/")
        };

        httpClientFactoryMock.CreateClient("datamuseApi").Returns(httpClient);

        var client = new DatamuseApiClient(httpClientFactoryMock);

        // Act
        var result = await client.CheckWord("test", "en");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Word);
        Assert.Empty(result.SimilarWords);
    }

    [Fact]
    public async Task CheckWord_ValidJsonResponse_ReturnsSimilarWords()
    {
        // Arrange
        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient(new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{\"word\":\"test\",\"score\":100}]")
            };
        }))
        {
            BaseAddress = new Uri("https://api.datamuse.com/")
        };

        httpClientFactoryMock.CreateClient("datamuseApi").Returns(httpClient);

        var client = new DatamuseApiClient(httpClientFactoryMock);

        // Act
        var result = await client.CheckWord("test", "en");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Word);
        Assert.Single(result.SimilarWords);
        Assert.Equal("test", result.SimilarWords.First().Word);
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
