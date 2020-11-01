using System.Collections.Generic;
using System.Linq;

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