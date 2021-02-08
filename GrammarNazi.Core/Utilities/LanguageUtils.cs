using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Enums;
using System;
using System.Globalization;
using System.Linq;

namespace GrammarNazi.Core.Utilities
{
    public static class LanguageUtils
    {
        /// <summary>
        /// Get TwoLetterISOLanguageName with the given ThreeLetterISOLanguageName
        /// </summary>
        /// <param name="langCode"></param>
        /// <returns>TwoLetterISOLanguageName</returns>
        public static string GetLanguageCode(string langCode)
        {
            var culture = CultureInfo.GetCultures(CultureTypes.AllCultures)
                            .FirstOrDefault(v => v.ThreeLetterISOLanguageName == langCode || v.ThreeLetterWindowsLanguageName.ToLower() == langCode);

            return culture.TwoLetterISOLanguageName;
        }

        /// <summary>
        /// Get string array of three letter supported languages
        /// </summary>
        /// <returns></returns>
        public static string[] GetSupportedLanguages()
        {
            return Enum.GetValues(typeof(SupportedLanguages))
                    .Cast<SupportedLanguages>()
                    .Select(v => v.GetLanguageInformation()?.ThreeLetterISOLanguageName)
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToArray();
        }
    }
}