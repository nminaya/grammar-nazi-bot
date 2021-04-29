using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Entities
{
    public struct SentimentAnalysisResult
    {
        public SentimentTypes Type { get; init; }
        public double Score { get; init; }
    }
}