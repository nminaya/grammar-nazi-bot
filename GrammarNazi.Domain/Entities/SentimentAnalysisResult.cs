using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Entities
{
    public class SentimentAnalysisResult
    {
        public SentimentTypes Type { get; set; }
        public double Score { get; set; }
    }
}