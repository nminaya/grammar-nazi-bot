using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.LanguageIdentificationAPI.SentimentAnalysis
{
    public class SentimentedConceptList
    {
        [JsonProperty("form")]
        public string Form { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("variant")]
        public string Variant { get; set; }

        [JsonProperty("inip")]
        public string Inip { get; set; }

        [JsonProperty("endp")]
        public string Endp { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("score_tag")]
        public string ScoreTag { get; set; }
    }
}