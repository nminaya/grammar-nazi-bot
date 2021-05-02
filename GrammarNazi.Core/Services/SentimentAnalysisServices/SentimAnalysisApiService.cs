using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class SentimentAnalysisService : ISentimentAnalysisService
    {
        private readonly ISentimApiClient _sentimApiClient;

        public SentimentAnalysisService(ISentimApiClient sentimApiClient)
        {
            _sentimApiClient = sentimApiClient;
        }

        public async Task<SentimentAnalysisResult> GetSentimentAnalysis(string text)
        {
            if (string.IsNullOrEmpty(text))
                return default;

            var analysisResult = await _sentimApiClient.GetSentimentResult(text);

            return new()
            {
                Score = analysisResult.Result.Polarity,
                Type = GetSentimentType(analysisResult.Result.Type)
            };
        }

        private static SentimentTypes GetSentimentType(string sentiment)
        {
            return sentiment.ToLower() switch
            {
                "positive" => SentimentTypes.Positive,
                "neutral" => SentimentTypes.Neutral,
                "negative" => SentimentTypes.Negative,
                _ => default
            };
        }
    }
}