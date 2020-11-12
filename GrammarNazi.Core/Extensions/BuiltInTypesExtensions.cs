using System;
using System.Collections.Generic;

namespace GrammarNazi.Core.Extensions
{
    public static class BuiltInTypesExtensions
    {
        public static string Join(this IEnumerable<string> list, string separator = ",") => string.Join(separator, list);

        public static bool IsAssignableToEnum<T>(this int val)
            where T : Enum
        {
            foreach (var item in Enum.GetValues(typeof(T)))
            {
                if (Convert.ToInt32(item) == val)
                    return true;
            }

            return false;
        }
    }
}