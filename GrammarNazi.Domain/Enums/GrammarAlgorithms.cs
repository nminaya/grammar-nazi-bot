using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GrammarNazi.Domain.Enums
{
    public enum GrammarAlgorithms
    {
        [Description("I̶n̶t̶e̶r̶n̶a̶l̶ ̶A̶l̶g̶o̶r̶i̶t̶h̶m̶ ̶(̶B̶E̶T̶A̶)̶")]
        InternalAlgorithm = 1,

        [Description("LanguageTool API")]
        LanguageToolApi = 2,

        [Description("YandexSpeller API")]
        YandexSpellerApi = 3
    }
}
