using GrammarNazi.Domain.Attributes;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
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

        public static LanguageInformation GetLanguageInformation(this SupportedLanguages language)
        {
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
    }
}