using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GrammarNazi.Core.Utilities
{
    public class CaseInsensitiveComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            if (x == null && y == null)
                return true;

            return x?.ToLower() == y?.ToLower();
        }

        public int GetHashCode([DisallowNull] string obj)
        {
            return obj.GetHashCode();
        }
    }
}