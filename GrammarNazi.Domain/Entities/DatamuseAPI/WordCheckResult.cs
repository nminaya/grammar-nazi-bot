using System;
using System.Collections.Generic;
using System.Linq;

namespace GrammarNazi.Domain.Entities.DatamuseAPI
{
    public class WordCheckResult
    {
        public string Word { get; set; }
        public IEnumerable<WordSimilarity> SimilarWords { get; set; }

        public bool IsWrongWord => SimilarWords.Any() && SimilarWords.All(v => !v.Word.Equals(Word, StringComparison.OrdinalIgnoreCase));
    }
}