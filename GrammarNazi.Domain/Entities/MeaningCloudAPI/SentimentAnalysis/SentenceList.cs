using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.MeaningCloudAPI.SentimentAnalysis;

public class SentenceList
{
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("inip")]
    public string Inip { get; set; }

    [JsonProperty("endp")]
    public string Endp { get; set; }

    [JsonProperty("bop")]
    public string Bop { get; set; }

    [JsonProperty("confidence")]
    public string Confidence { get; set; }

    [JsonProperty("score_tag")]
    public string ScoreTag { get; set; }

    [JsonProperty("agreement")]
    public string Agreement { get; set; }

    [JsonProperty("segment_list")]
    public List<SegmentList> SegmentList { get; set; }

    [JsonProperty("sentimented_entity_list")]
    public List<object> SentimentedEntityList { get; set; }

    [JsonProperty("sentimented_concept_list")]
    public List<SentimentedConceptList> SentimentedConceptList { get; set; }
}
