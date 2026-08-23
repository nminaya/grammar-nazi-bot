using GrammarNazi.Domain.Entities;

namespace GrammarNazi.Domain.Services;

public interface ISentimentAnalysisService
{
    Task<SentimentAnalysisResult> GetSentimentAnalysis(string text);
}
