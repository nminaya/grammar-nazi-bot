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
    /// Cerebras free tier quota is 30 requests per minute. 25 requests per minute leaves headroom to avoid hitting provider rate limits.
    /// </summary>
    public const int CerebrasRequestsPerMinute = 25;
    public const int CerebrasMaxRetries = 2;

    /// <summary>
    /// Groq API rate limit is set to 25 requests per minute to leave safety headroom under provider quotas.
    /// </summary>
    public const int GroqRequestsPerMinute = 25;
    public const int GroqMaxRetries = 2;
}
