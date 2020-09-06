using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GrammarNazi.Domain.Enums
{
    public enum GrammarAlgorithms
    {
        [Description("Internal Algorithm")]
        InternalAlgorithm = 1,

        [Description("LanguageTool API")]
        LanguageToolApi = 2
    }
}
