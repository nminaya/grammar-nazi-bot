using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Exceptions;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace GrammarNazi.Tests.Utilities;

public class ExternalApiResponseHelperTests
{
    [Fact]
    public void GetRetryAfter_AbsentHeader_ReturnsNull()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        // Act
        var result = ExternalApiResponseHelper.GetRetryAfter(response);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRetryAfter_DeltaHeader_ReturnsDelta()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(45));

        // Act
        var result = ExternalApiResponseHelper.GetRetryAfter(response);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromSeconds(45), result.Value);
    }

    [Fact]
    public void GetRetryAfter_FutureDateHeader_ReturnsPositiveDelta()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var futureDate = DateTimeOffset.UtcNow.AddSeconds(60);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(futureDate);

        // Act
        var result = ExternalApiResponseHelper.GetRetryAfter(response);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value > TimeSpan.Zero);
    }

    [Fact]
    public void GetRetryAfter_PastDateHeader_ClampsToZero()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var pastDate = DateTimeOffset.UtcNow.AddMinutes(-10);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(pastDate);

        // Act
        var result = ExternalApiResponseHelper.GetRetryAfter(response);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.Zero, result.Value);
    }

    [Fact]
    public void CreateExceptionForErrorResponse_PaymentRequired_ReturnsExternalApiQuotaExceededException()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.PaymentRequired);

        // Act
        var ex = ExternalApiResponseHelper.CreateExceptionForErrorResponse(response, "TestProvider", "test-model", "Payment required");

        // Assert
        var quotaEx = Assert.IsType<ExternalApiQuotaExceededException>(ex);
        Assert.Equal("TestProvider API quota exceeded or payment required.", quotaEx.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "{\"code\":\"payment_required\",\"message\":\"Payment required to access this resource.\"}")]
    [InlineData(HttpStatusCode.BadRequest, "{\"code\":\"insufficient_quota\",\"message\":\"You exceeded your current quota.\"}")]
    [InlineData(HttpStatusCode.TooManyRequests, "{\"code\":\"quota_exceeded\",\"message\":\"Quota exceeded for this account.\"}")]
    public void CreateExceptionForErrorResponse_QuotaErrorInBody_ReturnsExternalApiQuotaExceededException(HttpStatusCode statusCode, string errorBody)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode);

        // Act
        var ex = ExternalApiResponseHelper.CreateExceptionForErrorResponse(response, "TestProvider", "test-model", errorBody);

        // Assert
        var quotaEx = Assert.IsType<ExternalApiQuotaExceededException>(ex);
        Assert.Equal("TestProvider API quota exceeded or payment required.", quotaEx.Message);
    }

    [Fact]
    public void CreateExceptionForErrorResponse_TooManyRequests_ReturnsExternalApiRateLimitException()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(20));

        // Act
        var ex = ExternalApiResponseHelper.CreateExceptionForErrorResponse(response, "TestProvider", "test-model", "Rate limited");

        // Assert
        var rateLimitEx = Assert.IsType<ExternalApiRateLimitException>(ex);
        Assert.Equal("TestProvider API rate limit reached.", rateLimitEx.Message);
        Assert.NotNull(rateLimitEx.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(20), rateLimitEx.RetryAfter.Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    public void CreateExceptionForErrorResponse_TransientError_ReturnsExternalApiUnavailableException(HttpStatusCode statusCode)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode);

        // Act
        var ex = ExternalApiResponseHelper.CreateExceptionForErrorResponse(response, "TestProvider", "test-model", "Transient error");

        // Assert
        var unavailableEx = Assert.IsType<ExternalApiUnavailableException>(ex);
        Assert.Equal($"TestProvider API is currently unavailable ({statusCode}).", unavailableEx.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public void CreateExceptionForErrorResponse_PermanentError_ReturnsExternalApiPermanentFailureException(HttpStatusCode statusCode)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode);

        // Act
        var ex = ExternalApiResponseHelper.CreateExceptionForErrorResponse(response, "TestProvider", "test-model", "Permanent error");

        // Assert
        var permanentEx = Assert.IsType<ExternalApiPermanentFailureException>(ex);
        Assert.Equal($"TestProvider API rejected model 'test-model' ({statusCode}) — the model may have been retired or the API key may lack access. Retrying will not help.", permanentEx.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData((HttpStatusCode)418)]
    public void CreateExceptionForErrorResponse_OtherStatusCode_ReturnsInvalidOperationException(HttpStatusCode statusCode)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode);

        // Act
        var ex = ExternalApiResponseHelper.CreateExceptionForErrorResponse(response, "TestProvider", "test-model", "Other error");

        // Assert
        var invalidOpEx = Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal($"Unsuccessful TestProvider API response {statusCode}", invalidOpEx.Message);
    }
}
