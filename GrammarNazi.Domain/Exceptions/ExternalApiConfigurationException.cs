using System;

namespace GrammarNazi.Domain.Exceptions;

public class ExternalApiConfigurationException : Exception
{
    public ExternalApiConfigurationException()
    {
    }

    public ExternalApiConfigurationException(string message) : base(message)
    {
    }

    public ExternalApiConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
