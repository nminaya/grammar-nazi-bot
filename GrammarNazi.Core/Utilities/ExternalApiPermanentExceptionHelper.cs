
namespace GrammarNazi.Core.Utilities;

public static class ExternalApiPermanentExceptionHelper
{
    private static readonly string[] PermanentFailureKeywords =
    [
        "model_not_found",
        "model_decommissioned",
        "model_not_active",
        "invalid_api_key",
        "API_KEY_INVALID",
        "PERMISSION_DENIED"
    ];

    private static readonly string[] QuotaExceededKeywords =
    [
        "payment_required",
        "insufficient_quota",
        "quota_exceeded"
    ];

    public static bool IsPermanentFailure(string errorContent)
    {
        if (string.IsNullOrEmpty(errorContent))
        {
            return false;
        }

        return PermanentFailureKeywords.Any(keyword => errorContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsQuotaExceeded(string errorContent)
    {
        if (string.IsNullOrEmpty(errorContent))
        {
            return false;
        }

        return QuotaExceededKeywords.Any(keyword => errorContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
