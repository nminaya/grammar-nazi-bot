using System.ComponentModel;

namespace GrammarNazi.Domain.Enums;

public enum GrammarAlgorithms
{
    [Description("Internal Algorithm (BETA)")]
    InternalAlgorithm = 1,

    [Description("LanguageTool API")]
    LanguageToolApi = 2,

    [Description("YandexSpeller API")]
    YandexSpellerApi = 3,

    [Description("Datamuse API")]
    DatamuseApi = 4
}
