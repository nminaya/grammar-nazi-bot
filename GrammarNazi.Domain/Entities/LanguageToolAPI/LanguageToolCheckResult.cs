using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI;

public class LanguageToolCheckResult
{
    [JsonProperty("software")]
    public Software Software { get; set; }

    [JsonProperty("warnings")]
    public Warnings Warnings { get; set; }

    [JsonProperty("language")]
    public Language Language { get; set; }

    [JsonProperty("matches")]
    public List<Match> Matches { get; set; }
}
