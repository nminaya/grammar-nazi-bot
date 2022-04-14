using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities;

/// <summary>
/// Represents the grammar correction of a word or part of a text
/// </summary>
public class GrammarCorrection
{
    /// <summary>
    /// The grammatically wrong word
    /// </summary>
    public string WrongWord { get; set; }

    /// <summary>
    /// Error message related to the wrong word
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// List of possible replacements for the wrong word
    /// </summary>
    public IEnumerable<string> PossibleReplacements { get; set; }
}
