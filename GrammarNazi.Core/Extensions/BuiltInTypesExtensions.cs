using System;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<string> SplitInParts(this string str, int partLength)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (int i = 0; i < str.Length; i += partLength)
            {
                yield return str.Substring(i, Math.Min(partLength, str.Length - i));
            }
        }

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
    }
}