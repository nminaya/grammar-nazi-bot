using GrammarNazi.Domain.Attributes;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using System;
using System.ComponentModel;
using System.Linq;

namespace GrammarNazi.Core.Extensions;

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

    public static LanguageInformation GetLanguageInformation(this SupportedLanguages language)
    {
        if (language == SupportedLanguages.Auto)
            return default;

        var langInfo = language
            .GetType()
            .GetField(language.ToString())
            .GetCustomAttributes(typeof(LanguageInformationAttribute), false)
            .Cast<LanguageInformationAttribute>()
            .FirstOrDefault();

        if (langInfo == default)
            throw new InvalidOperationException($"SupportedLanguages.{language} does not have LanguageInformation attribute");

        return new()
        {
            TwoLetterISOLanguageName = langInfo.TwoLetterISOLanguageName,
            ThreeLetterISOLanguageName = langInfo.ThreeLetterISOLanguageName
        };
    }

    public static bool IsLanguageSupported(this GrammarAlgorithms algorithm, SupportedLanguages language)
    {
        return algorithm switch
        {
            GrammarAlgorithms.InternalAlgorithm => true,
            GrammarAlgorithms.LanguageToolApi => true,
            GrammarAlgorithms.DatamuseApi => new[] { SupportedLanguages.English, SupportedLanguages.Spanish }.Contains(language),
            GrammarAlgorithms.YandexSpellerApi => new[] { SupportedLanguages.English, SupportedLanguages.Spanish }.Contains(language),
            _ => true,
        };
    }
}