using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Entities
{
    public class SentimentAnalysisResult
    {
        public SentimentTypes SentimentType { get; set; }
        public double Score { get; set; }
    }
}