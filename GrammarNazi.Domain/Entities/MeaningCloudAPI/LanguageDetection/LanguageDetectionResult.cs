using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.MeaningCloudAPI;

public class LanguageDetectionResult
{
    [JsonProperty("status")]
    public MeganingStatus Status { get; set; }

    [JsonProperty("language_list")]
    public IEnumerable<DetectedLanguage> LanguageList { get; set; }
}
