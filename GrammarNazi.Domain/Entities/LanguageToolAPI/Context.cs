using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI;

public class Context
{
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("offset")]
    public int Offset { get; set; }

    [JsonProperty("length")]
    public int Length { get; set; }
}
