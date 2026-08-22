using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Exceptions;
using System.Net;
using Xunit;

namespace GrammarNazi.Tests.Resilience;

public class ResilienceHandlerTests
{
    [Fact]
    public async Task SendAsync_ExceedsPermitLimit_ThrowsExternalApiRateLimitException_And_InnerHandlerSawNRequests()
    {
        // Arrange
        const int n = 5;
        int innerHandlerCalls = 0;
        var stubInnerHandler = new StubHttpMessageHandler(req =>
        {
            innerHandlerCalls++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var limiter = new ServiceCollectionExtensions.SlidingWindowRateLimiter(n, TimeSpan.FromMinutes(1));
        var pipeline = ServiceCollectionExtensions.CreateApiResiliencePipeline(maxRetries: 2);
        var handler = new ServiceCollectionExtensions.ApiResilienceHandler(limiter, pipeline)
        {
            InnerHandler = stubInnerHandler
        };

        using var client = new HttpClient(handler);

        // Act: Send N requests
        for (int i = 0; i < n; i++)
        {
            var response = await client.GetAsync("https://example.com/test");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Act & Assert: (N + 1)-th request throws rate limit exception
        await Assert.ThrowsAsync<ExternalApiRateLimitException>(() => client.GetAsync("https://example.com/test"));

        // Inner handler saw exactly N requests
        Assert.Equal(n, innerHandlerCalls);
    }

    [Fact]
    public async Task SendAsync_RetriesConsumePermits_InnerHandlerInvokedAtMostPermitLimit()
    {
        // Arrange: Permit limit = 2, max retries = 2
        const int permitLimit = 2;
        int innerHandlerCalls = 0;
        var stubInnerHandler = new StubHttpMessageHandler(req =>
        {
            innerHandlerCalls++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable); // 503 triggers retry
        });

        var limiter = new ServiceCollectionExtensions.SlidingWindowRateLimiter(permitLimit, TimeSpan.FromMinutes(1));
        var pipeline = ServiceCollectionExtensions.CreateApiResiliencePipeline(maxRetries: 2);
        var handler = new ServiceCollectionExtensions.ApiResilienceHandler(limiter, pipeline)
        {
            InnerHandler = stubInnerHandler
        };

        using var client = new HttpClient(handler);

        // Act: Single logical call that would attempt initial + 2 retries (3 attempts total),
        // but permit budget is 2, so the 3rd attempt is rejected by rate limiter inside pipeline execution.
        await Assert.ThrowsAsync<ExternalApiRateLimitException>(() => client.GetAsync("https://example.com/test"));

        // Assert: Inner handler was invoked at most permitLimit (2) times
        Assert.Equal(permitLimit, innerHandlerCalls);
    }

    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _func;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> func)
        {
            _func = func;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_func(request));
        }
    }
}
