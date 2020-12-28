using Newtonsoft.Json;

namespace GrammarNazi.Domain.Entities.DatamuseAPI
{
    /// <summary>
    /// Datamuse API Word Similarity
    /// </summary>
    public class WordSimilarity
    {
        /// <summary>
        /// Word
        /// </summary>
        [JsonProperty("word")]
        public string Word { get; set; }

        /// <summary>
        /// Similarity score
        /// </summary>
        [JsonProperty("score")]
        public int Score { get; set; }
    }
}