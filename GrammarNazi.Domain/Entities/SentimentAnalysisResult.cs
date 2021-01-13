using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Entities
{
    public class SentimentAnalysisResult
    {
        public SentimentTypes Type { get; init; }
        public double Score { get; init; }
    }
}