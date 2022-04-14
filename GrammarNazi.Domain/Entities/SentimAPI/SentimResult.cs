using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GrammarNazi.Domain.Entities.SentimAPI;

public class SentimResult
{
    [JsonPropertyName("result")]
    public SentimentCheckResult Result { get; set; }

    [JsonPropertyName("sentences")]
    public List<SentimSentence> Sentences { get; set; }
}
