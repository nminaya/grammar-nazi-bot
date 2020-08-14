using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class Replacement
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
