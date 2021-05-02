using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.MeaningCloudAPI
{
    public class DetectedLanguage
    {
        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("relevance")]
        public int Relevance { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("iso639-3")]
        public string Iso6393 { get; set; }

        [JsonProperty("iso639-2")]
        public string Iso6392 { get; set; }
    }
}