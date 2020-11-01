using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.LanguageToolAPI
{
    public class Type
    {
        [JsonProperty("typeName")]
        public string TypeName { get; set; }
    }
}