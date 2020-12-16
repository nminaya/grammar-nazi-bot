using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.DatamuseAPI
{
    public class WordCheckResult
    {
        public string Word { get; set; }
        public IEnumerable<WordCheck> Words { get; set; }
    }
}