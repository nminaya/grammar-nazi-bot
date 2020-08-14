using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class Language
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("detectedLanguage")]
        public DetectedLanguage DetectedLanguage { get; set; }
    }
}
