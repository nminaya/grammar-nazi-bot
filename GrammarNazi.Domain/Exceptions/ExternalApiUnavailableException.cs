using System;
using System.Runtime.Serialization;

namespace GrammarNazi.Domain.Exceptions;

public class ExternalApiUnavailableException : Exception
{
    public ExternalApiUnavailableException()
    {
    }

    public ExternalApiUnavailableException(string message) : base(message)
    {
    }

    public ExternalApiUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
