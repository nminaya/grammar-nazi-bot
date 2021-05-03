using GrammarNazi.Domain.Entities.MeaningCloudAPI.SentimentAnalysis;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.MeaningCloudAPI
{
    public class SentimentAnalysisResult
    {
        [JsonProperty("status")]
        public MeganingStatus Status { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("score_tag")]
        public string ScoreTag { get; set; }

        [JsonProperty("agreement")]
        public string Agreement { get; set; }

        [JsonProperty("subjectivity")]
        public string Subjectivity { get; set; }

        [JsonProperty("confidence")]
        public string Confidence { get; set; }

        [JsonProperty("irony")]
        public string Irony { get; set; }

        [JsonProperty("sentence_list")]
        public List<SentenceList> SentenceList { get; set; }

        [JsonProperty("sentimented_concept_list")]
        public List<SentimentedConceptList> SentimentedConceptList { get; set; }
    }
}