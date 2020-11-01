using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class Match
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("shortMessage")]
        public string ShortMessage { get; set; }

        [JsonProperty("replacements")]
        public List<Replacement> Replacements { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("context")]
        public Context Context { get; set; }

        [JsonProperty("sentence")]
        public string Sentence { get; set; }

        [JsonProperty("type")]
        public Type Type { get; set; }

        [JsonProperty("rule")]
        public Rule Rule { get; set; }

        [JsonProperty("ignoreForIncompleteSentence")]
        public bool IgnoreForIncompleteSentence { get; set; }

        [JsonProperty("contextForSureMatch")]
        public int ContextForSureMatch { get; set; }
    }
}