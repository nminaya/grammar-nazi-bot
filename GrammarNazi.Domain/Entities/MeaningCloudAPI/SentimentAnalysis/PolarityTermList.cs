using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.LanguageIdentificationAPI.SentimentAnalysis
{
    public class PolarityTermList
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("inip")]
        public string Inip { get; set; }

        [JsonProperty("endp")]
        public string Endp { get; set; }

        [JsonProperty("confidence")]
        public string Confidence { get; set; }

        [JsonProperty("score_tag")]
        public string ScoreTag { get; set; }

        [JsonProperty("sentimented_concept_list")]
        public List<SentimentedConceptList> SentimentedConceptList { get; set; }
    }
}