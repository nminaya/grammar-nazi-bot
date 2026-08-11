using System;
using System.Linq;

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

    public static bool IsPermanentFailure(string errorContent)
    {
        if (string.IsNullOrEmpty(errorContent))
        {
            return false;
        }

        return PermanentFailureKeywords.Any(keyword => errorContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
