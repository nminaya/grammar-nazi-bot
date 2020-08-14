using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class Type
    {
        [JsonProperty("typeName")]
        public string TypeName { get; set; }
    }
}
