using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI;

public class Replacement
{
    [JsonProperty("value")]
    public string Value { get; set; }
}
