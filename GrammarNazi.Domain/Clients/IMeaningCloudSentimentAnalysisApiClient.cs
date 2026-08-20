using GrammarNazi.Domain.Entities.MeaningCloudAPI;

namespace GrammarNazi.Domain.Clients;

public interface IMeaningCloudSentimentAnalysisApiClient
{
    Task<SentimentAnalysisResult> GetSentimentResult(string text, string language);
}