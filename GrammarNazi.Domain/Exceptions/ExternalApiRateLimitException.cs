
namespace GrammarNazi.Domain.Exceptions;

public class ExternalApiRateLimitException : Exception
{
    public TimeSpan? RetryAfter { get; }

    public ExternalApiRateLimitException()
    {
    }

    public ExternalApiRateLimitException(string message) : base(message)
    {
    }

    public ExternalApiRateLimitException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ExternalApiRateLimitException(string message, TimeSpan? retryAfter) : base(message)
    {
        RetryAfter = retryAfter;
    }

    public ExternalApiRateLimitException(string message, TimeSpan? retryAfter, Exception innerException) : base(message, innerException)
    {
        RetryAfter = retryAfter;
    }
}
