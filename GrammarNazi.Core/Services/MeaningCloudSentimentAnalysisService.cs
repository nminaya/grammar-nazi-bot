using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class MeaningCloudSentimentAnalysisService : ISentimentAnalysisService
    {
        private readonly IMeaningCloudSentimentAnalysisApiClient _meaningCloudSentimentAnalysisApiClient;
        private readonly ILanguageService _languageService;

        public MeaningCloudSentimentAnalysisService(IMeaningCloudSentimentAnalysisApiClient meaningCloudSentimentAnalysisApiClient,
            ILanguageService languageService)
        {
            _meaningCloudSentimentAnalysisApiClient = meaningCloudSentimentAnalysisApiClient;
            _languageService = languageService;
        }

        public async Task<SentimentAnalysisResult> GetSentimentAnalysis(string text)
        {
            var language = _languageService.IdentifyLanguage(text);

            var sentimentAnalysisResult = await _meaningCloudSentimentAnalysisApiClient.GetSentimentResult(text, language.TwoLetterISOLanguageName);

            if (sentimentAnalysisResult.Status.RemainingCredits == 0 || !sentimentAnalysisResult.SentenceList.Any())
                return default;

            return new()
            {
                Type = GetSentimentType(sentimentAnalysisResult.ScoreTag),
                Score = GetSentimentScore(sentimentAnalysisResult.ScoreTag)
            };
        }

        private static SentimentTypes GetSentimentType(string scoreTag)
        {
            return scoreTag == "NONE" ? SentimentTypes.Neutral
                : scoreTag.Contains("P") ? SentimentTypes.Positive
                : SentimentTypes.Negative;
        }

        private static double GetSentimentScore(string scoreTag)
        {
            return scoreTag switch
            {
                "P+" => 1,
                "P" => 0.75,
                "NONE" => 0.5,
                "N" => 0.25,
                _ => 0
            };
        }
    }
}