using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.MeaningCloudAPI;

public class MeganingStatus
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("msg")]
    public string Msg { get; set; }

    [JsonProperty("credits")]
    public string Credits { get; set; }

    [JsonProperty("remaining_credits")]
    public int RemainingCredits { get; set; }
}
