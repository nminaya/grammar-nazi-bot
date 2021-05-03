using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.MeaningCloudAPI.SentimentAnalysis
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