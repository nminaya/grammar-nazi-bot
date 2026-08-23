using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GrammarNazi.Core.Utilities;

public static class EnumUtils
{
    public static IEnumerable<T> GetEnabledValues<T>()
        where T : struct, Enum
    {
        return Enum.GetValues<T>().Where(v => !v.IsDisabled);
    }

    public static string GetAvailableOptions<T>(T selectedOption)
        where T : struct, Enum
    {
        var options = GetEnabledValues<T>();

        var messageBuilder = new StringBuilder();

        foreach (var item in options)
        {
            var selected = item.Equals(selectedOption) ? "✅" : "";
            messageBuilder.AppendLine($"{Convert.ToInt32(item)} - {item.Description} {selected}");
        }

        return messageBuilder.ToString();
    }

    public static string GetUnsupportedLanguageWarning(SupportedLanguages language, GrammarAlgorithms algorithm)
    {
        return $"WARNING: The selected language ({language.Description}) is not supported by the selected algorithm ({algorithm.Description}).";
    }
}
