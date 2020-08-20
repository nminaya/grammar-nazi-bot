using System;
using System.ComponentModel;
using System.Linq;

namespace GrammarNazi.Core.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription<T>(this T @enum)
            where T : Enum
        {
            var attributes = @enum
                               .GetType()
                               .GetField(@enum.ToString())
                               .GetCustomAttributes(typeof(DescriptionAttribute), false)
                               .Cast<DescriptionAttribute>()
                               .ToArray();

            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }
    }
}