using GrammarNazi.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GrammarNazi.Core.Utilities;

public static class EnumUtils
{
    public static IEnumerable<T> GetEnabledValues<T>()
        where T : Enum
    {
        return Enum.GetValues(typeof(T))
                   .Cast<T>()
                   .Where(v => !v.IsDisabled());
    }
}
