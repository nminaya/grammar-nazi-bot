
namespace GrammarNazi.Domain.Exceptions;

public class ExternalApiQuotaExceededException : Exception
{
    public ExternalApiQuotaExceededException()
    {
    }

    public ExternalApiQuotaExceededException(string message) : base(message)
    {
    }

    public ExternalApiQuotaExceededException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
