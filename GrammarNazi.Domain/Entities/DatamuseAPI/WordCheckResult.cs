using System;
using System.Collections.Generic;
using System.Linq;

namespace GrammarNazi.Domain.Entities.DatamuseAPI
{
    /// <summary>
    /// Datamuse API Word Check
    /// </summary>
    public class WordCheckResult
    {
        /// <summary>
        /// Word to Check
        /// </summary>
        public string Word { get; set; }

        /// <summary>
        /// List of words that are spelled similarly to 'Word'
        /// </summary>
        public IEnumerable<WordSimilarity> SimilarWords { get; set; }

        /// <summary>
        /// True if 'SimilarWord' doesn't contains 'Word'
        /// </summary>
        public bool IsWrongWord => SimilarWords.Any() && SimilarWords.All(v => !v.Word.Equals(Word, StringComparison.OrdinalIgnoreCase));
    }
}