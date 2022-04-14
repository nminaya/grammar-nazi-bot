using GrammarNazi.Domain.Attributes;
using System.ComponentModel;

namespace GrammarNazi.Domain.Enums;

public enum SupportedLanguages
{
    [Description("Auto")]
    Auto = 0,

    [Description("English")]
    [LanguageInformation("en", "eng")]
    English = 1,

    [Description("Spanish")]
    [LanguageInformation("es", "spa")]
    Spanish = 2
}
