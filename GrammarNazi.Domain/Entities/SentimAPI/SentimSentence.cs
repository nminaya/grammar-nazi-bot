using System.Text.Json.Serialization;

namespace GrammarNazi.Domain.Entities.SentimAPI
{
    public class SentimSentence
    {
        [JsonPropertyName("sentence")]
        public string Sentence { get; set; }

        [JsonPropertyName("sentiment")]
        public SentimentCheckResult Sentiment { get; set; }
    }
}