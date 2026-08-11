using System;

namespace GrammarNazi.Domain.Exceptions;

public class ExternalApiPermanentFailureException : Exception
{
    public ExternalApiPermanentFailureException()
    {
    }

    public ExternalApiPermanentFailureException(string message) : base(message)
    {
    }

    public ExternalApiPermanentFailureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
