using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Constants;

public static class Defaults
{
    public const string LanguageCode = "en";
    public const int LanguageToolApiMaxTextLength = 1_500;
    public const int StringComparableRange = 2;
    public const GrammarAlgorithms DefaultAlgorithm = GrammarAlgorithms.GroqApi;
    public const string TelegramBotUser = "grammarNz_Bot";
    public const double ValidPositiveSentimentScore = 0.60;
    public const int GithubIssueMaxTitleLength = 256;
    public const int TwitterTextMaxLength = 280;
    public const int DiscordTextMaxLength = 2_000;

    /// <summary>
    /// Cerebras API free tier quota is 30 requests per minute for standard models. Setting to 25 requests per minute leaves safety headroom to avoid hitting provider rate limits.
    /// </summary>
    public const int CerebrasRequestsPerMinute = 25;
    public const int CerebrasMaxRetries = 2;

    /// <summary>
    /// Groq API free tier quota is 30 requests per minute for standard models. Setting to 25 requests per minute leaves safety headroom under provider quotas.
    /// </summary>
    public const int GroqRequestsPerMinute = 25;
    public const int GroqMaxRetries = 2;

    /// <summary>
    /// Gemini API free tier quota is assumed to be 15 requests per minute (e.g. for Gemini 1.5 Flash models configured via GEMINI_MODEL_VERSION). Setting to 12 requests per minute leaves safety headroom to avoid hitting provider rate limits. Revisit if GEMINI_MODEL_VERSION is pointed to a different model family with different quota limits.
    /// </summary>
    public const int GeminiRequestsPerMinute = 12;
    public const int GeminiMaxRetries = 2;
}
