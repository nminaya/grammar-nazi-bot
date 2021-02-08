using GrammarNazi.Domain.Attributes;
using System.ComponentModel;

namespace GrammarNazi.Domain.Enums
{
    public enum SupportedLanguages
    {
        Auto = 0,

        [LanguageInformation("en", "eng")]
        English = 1,

        [LanguageInformation("es", "spa")]
        Spanish = 2
    }
}