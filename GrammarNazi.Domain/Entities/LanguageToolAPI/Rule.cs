using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class Rule
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("issueType")]
        public string IssueType { get; set; }

        [JsonProperty("category")]
        public Category Category { get; set; }
    }
}
