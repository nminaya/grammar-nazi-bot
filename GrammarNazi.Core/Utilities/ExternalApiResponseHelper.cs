using GrammarNazi.Domain.Exceptions;
using System;
using System.Net;
using System.Net.Http;

namespace GrammarNazi.Core.Utilities;

public static class ExternalApiResponseHelper
{
    public static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter == null) return null;

        if (retryAfter.Delta.HasValue)
        {
            return retryAfter.Delta.Value < TimeSpan.Zero ? TimeSpan.Zero : retryAfter.Delta.Value;
        }

        if (retryAfter.Date.HasValue)
        {
            var delta = retryAfter.Date.Value - DateTimeOffset.UtcNow;
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        return null;
    }

    public static Exception CreateExceptionForErrorResponse(HttpResponseMessage response, string providerName, string model, string errorContent)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new ExternalApiRateLimitException(
                $"{providerName} API rate limit reached.",
                GetRetryAfter(response),
                new Exception(errorContent));
        }

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable
            || response.StatusCode == HttpStatusCode.BadGateway
            || response.StatusCode == HttpStatusCode.GatewayTimeout
            || response.StatusCode == HttpStatusCode.InternalServerError
            || response.StatusCode == HttpStatusCode.RequestTimeout)
        {
            return new ExternalApiUnavailableException(
                $"{providerName} API is currently unavailable ({response.StatusCode}).",
                new Exception(errorContent));
        }

        if (response.StatusCode == HttpStatusCode.NotFound
            || response.StatusCode == HttpStatusCode.Unauthorized
            || response.StatusCode == HttpStatusCode.Forbidden
            || (response.StatusCode == HttpStatusCode.BadRequest && ExternalApiPermanentExceptionHelper.IsPermanentFailure(errorContent)))
        {
            return new ExternalApiPermanentFailureException(
                $"{providerName} API rejected model '{model}' ({response.StatusCode}) — the model may have been retired or the API key may lack access. Retrying will not help.",
                new Exception(errorContent));
        }

        return new InvalidOperationException($"Unsuccessful {providerName} API response {response.StatusCode}", new Exception(errorContent));
    }
}
