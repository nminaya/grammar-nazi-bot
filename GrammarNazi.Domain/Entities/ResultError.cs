using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities
{
    public class ResultError
    {
        public string WrongWord { get; set; }
        public IEnumerable<string> PossibleReplacements { get; set; }
    }
}
