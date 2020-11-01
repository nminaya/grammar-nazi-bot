using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class Warnings
    {
        [JsonProperty("incompleteResults")]
        public bool IncompleteResults { get; set; }
    }
}