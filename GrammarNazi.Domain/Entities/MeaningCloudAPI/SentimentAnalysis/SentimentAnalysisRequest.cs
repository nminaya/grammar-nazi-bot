using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.LanguageIdentificationAPI.SentimentAnalysis
{
    public class SentimentAnalysisRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("txt")]
        public string Text { get; set; }
    }
}