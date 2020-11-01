using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Constants
{
    public static class Defaults
    {
        public const string LanguageCode = "en";
        public const int MaxLengthText = 10_000;
        public const int StringComparableRange = 2;
        public const GrammarAlgorithms DefaultAlgorithm = GrammarAlgorithms.LanguageToolApi;
    }
}