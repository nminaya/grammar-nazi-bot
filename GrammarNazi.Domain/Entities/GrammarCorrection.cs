using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities
{
    public class GrammarCorrection
    {
        public string WrongWord { get; set; }
        public string Message { get; set; }
        public IEnumerable<string> PossibleReplacements { get; set; }
    }
}