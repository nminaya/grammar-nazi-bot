using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GrammarNazi.Domain.Enums
{
    public enum GrammarAlgorithms
    {
        [Description("Internal Algorithm (BETA)")]
        InternalAlgorithm = 1,

        [Description("LanguageTool API")]
        LanguageToolApi = 2,

        [Description("Yandex Speller API")]
        YandexSpellerApi = 3
    }
}
