using System.Text.Json.Serialization;

namespace GrammarNazi.Domain.Entities.SentimAPI;

public class SentimentCheckResult
{
    [JsonPropertyName("polarity")]
    public double Polarity { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}
