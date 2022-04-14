using System.ComponentModel;

namespace GrammarNazi.Domain.Enums;

public enum CorrectionStrictnessLevels
{
    [Description("Intolerant: Corrects every single possible error found")]
    Intolerant = 1,

    [Description("Tolerant: It will ignore some basic common errors")]
    Tolerant = 2
}
