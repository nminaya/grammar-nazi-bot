using GrammarNazi.Domain.Entities.LanguageIdentificationAPI;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients
{
    public interface IMeaningCloudSentimentAnalysisApiClient
    {
        Task<SentimentAnalysisResult> GetSentimentResult(string text, string language);
    }
}