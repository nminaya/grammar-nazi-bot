using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.MeaningCloudAPI.SentimentAnalysis
{
    public class SegmentList
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("segment_type")]
        public string SegmentType { get; set; }

        [JsonProperty("inip")]
        public string Inip { get; set; }

        [JsonProperty("endp")]
        public string Endp { get; set; }

        [JsonProperty("confidence")]
        public string Confidence { get; set; }

        [JsonProperty("score_tag")]
        public string ScoreTag { get; set; }

        [JsonProperty("agreement")]
        public string Agreement { get; set; }

        [JsonProperty("polarity_term_list")]
        public List<PolarityTermList> PolarityTermList { get; set; }
    }
}