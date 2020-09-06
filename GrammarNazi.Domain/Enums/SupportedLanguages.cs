using System.ComponentModel;

namespace GrammarNazi.Domain.Enums
{
    public enum SupportedLanguages
    {
        Auto = 0,

        [Description("eng")]
        English = 1,

        [Description("spa")]
        Spanish = 2
    }
}