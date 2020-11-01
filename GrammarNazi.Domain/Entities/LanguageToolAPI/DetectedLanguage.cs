using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class DetectedLanguage
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }
}