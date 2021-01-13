using GrammarNazi.Domain.Entities;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface ISentimentAnalysisService
    {
        Task<SentimentAnalysisResult> GetSentimentAnalysis(string text);
    }
}