using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.DatamuseAPI
{
    public class WordCheck
    {
        [JsonProperty("word")]
        public string Word { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }
    }
}