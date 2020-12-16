using System;
using System.Collections.Generic;
using System.Linq;

namespace GrammarNazi.Domain.Entities.DatamuseAPI
{
    public class WordCheckResult
    {
        public string Word { get; set; }
        public IEnumerable<WordCheck> Words { get; set; }

        public bool HasCorrections => Words.Any() && Words.All(v => !v.Word.Equals(Word, StringComparison.OrdinalIgnoreCase));
    }
}