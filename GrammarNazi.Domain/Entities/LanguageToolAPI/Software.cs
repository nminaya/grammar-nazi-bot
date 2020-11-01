using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class Software
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("buildDate")]
        public string BuildDate { get; set; }

        [JsonProperty("apiVersion")]
        public int ApiVersion { get; set; }

        [JsonProperty("premium")]
        public bool Premium { get; set; }

        [JsonProperty("premiumHint")]
        public string PremiumHint { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}