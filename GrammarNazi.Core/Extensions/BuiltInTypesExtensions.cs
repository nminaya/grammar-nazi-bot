using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GrammarNazi.Core.Extensions
{
    public static class BuiltInTypesExtensions
    {
        public static string Join(this IEnumerable<string> list, string separator = ",") => string.Join(separator, list);
    }
}
