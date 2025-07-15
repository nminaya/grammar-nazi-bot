using System;
using System.Runtime.Serialization;

namespace GrammarNazi.Domain.Exceptions;

public class GeminiServiceUnavailableException : Exception
{
    public GeminiServiceUnavailableException()
    {
    }

    public GeminiServiceUnavailableException(string message) : base(message)
    {
    }

    public GeminiServiceUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
