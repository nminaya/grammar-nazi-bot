using System;

namespace GrammarNazi.Domain.Exceptions;

public class GroqRateLimitException : Exception
{
    public GroqRateLimitException()
    {
    }

    public GroqRateLimitException(string message) : base(message)
    {
    }

    public GroqRateLimitException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
