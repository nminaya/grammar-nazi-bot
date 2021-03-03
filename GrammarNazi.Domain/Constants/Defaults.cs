using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Constants
{
    public static class Defaults
    {
        public const string LanguageCode = "en";
        public const int LanguageToolApiMaxTextLength = 1_500;
        public const int StringComparableRange = 2;
        public const GrammarAlgorithms DefaultAlgorithm = GrammarAlgorithms.LanguageToolApi;
        public const string TelegramBotUser = "grammarNz_Bot";
        public const double ValidPositiveSentimentScore = 0.60;
        public const int GithubIssueMaxTitleLength = 256;
        public const int TwitterTextMaxLength = 280;
        public const int DiscordTextMaxLength = 2_000;
    }
}