using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class Warnings
    {
        [JsonProperty("incompleteResults")]
        public bool IncompleteResults { get; set; }
    }
}
