using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI;

public class Category
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}
