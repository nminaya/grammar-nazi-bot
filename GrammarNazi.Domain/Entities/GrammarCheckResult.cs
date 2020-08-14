using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GrammarNazi.Domain.Entities
{
    public class GrammarCheckResult
    {
        public bool HasCorrections => Corrections?.Any() == true;

        public IEnumerable<GrammarCorrection> Corrections { get; }

        public GrammarCheckResult(IEnumerable<GrammarCorrection> corrections)
        {
            Corrections = corrections;
        }
    }
}
