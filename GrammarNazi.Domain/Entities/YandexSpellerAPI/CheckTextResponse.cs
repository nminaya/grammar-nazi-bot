using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.YandexSpellerAPI
{
    public class CheckTextResponse
    {
        [JsonProperty("code")]
        public YandexSpellerErrorCodes ErrorCode { get; set; }

        [JsonProperty("pos")]
        public int Pos { get; set; }

        [JsonProperty("row")]
        public int Row { get; set; }

        [JsonProperty("col")]
        public int Col { get; set; }

        [JsonProperty("len")]
        public int Len { get; set; }

        [JsonProperty("word")]
        public string Word { get; set; }

        [JsonProperty("s")]
        public List<string> Replacements { get; set; }
    }
}