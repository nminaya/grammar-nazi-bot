using GrammarNazi.Domain.Attributes;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using System.ComponentModel;

namespace GrammarNazi.Core.Extensions;

public static class EnumExtensions
{
    extension<T>(T @enum) where T : Enum
    {
        public string Description
        {
            get
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

        public bool IsDisabled =>
            @enum.GetType()
                 .GetField(@enum.ToString())
                 .GetCustomAttributes(typeof(DisabledAttribute), false)
                 .Length > 0;
    }

    extension(GrammarAlgorithms algorithm)
    {
        public bool IsLanguageSupported(SupportedLanguages language)
        {
            return algorithm switch
            {
                GrammarAlgorithms.InternalAlgorithm => true,
                GrammarAlgorithms.LanguageToolApi => true,
                GrammarAlgorithms.DatamuseApi => language is SupportedLanguages.English or SupportedLanguages.Spanish,
                GrammarAlgorithms.YandexSpellerApi => language is SupportedLanguages.English or SupportedLanguages.Spanish,
                _ => true,
            };
        }
    }

    extension(SupportedLanguages language)
    {
        public LanguageInformation GetLanguageInformation()
        {
            if (language == SupportedLanguages.Auto)
            {
                return default;
            }

            var langInfo = language
                .GetType()
                .GetField(language.ToString())
                .GetCustomAttributes(typeof(LanguageInformationAttribute), false)
                .Cast<LanguageInformationAttribute>()
                .FirstOrDefault();

            if (langInfo == default)
            {
                throw new InvalidOperationException(
                    $"SupportedLanguages.{language} does not have LanguageInformation attribute");
            }

            return new()
            {
                TwoLetterISOLanguageName = langInfo.TwoLetterISOLanguageName,
                ThreeLetterISOLanguageName = langInfo.ThreeLetterISOLanguageName
            };
        }
    }
}
