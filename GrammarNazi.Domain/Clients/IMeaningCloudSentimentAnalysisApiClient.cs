using GrammarNazi.Domain.Entities.MeaningCloudAPI;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients;

public interface IMeaningCloudSentimentAnalysisApiClient
{
    Task<SentimentAnalysisResult> GetSentimentResult(string text, string language);
}