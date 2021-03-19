using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GrammarNazi.Core.Utilities
{
    public class CaseInsensitiveEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x?.ToLower() == y?.ToLower();
        }

        public int GetHashCode([DisallowNull] string obj)
        {
            return obj.GetHashCode();
        }
    }
}