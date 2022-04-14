using System.Collections.Generic;
using System.Linq;

namespace GrammarNazi.Domain.Entities;

/// <summary>
/// Represents the result of Grammar check
/// </summary>
public class GrammarCheckResult
{
    /// <summary>
    /// True if there is any Correction
    /// </summary>
    public bool HasCorrections => Corrections?.Any() == true;

    /// <summary>
    /// List of Corrections
    /// </summary>
    public IEnumerable<GrammarCorrection> Corrections { get; }

    public GrammarCheckResult(IEnumerable<GrammarCorrection> corrections)
    {
        Corrections = corrections;
    }
}
