using System;

namespace GrammarNazi.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LanguageInformationAttribute : Attribute
    {
        /// <summary>
        /// The ISO 639-2 two-letter language code
        /// </summary>
        public string TwoLetterISOLanguageName { get; }

        /// <summary>
        /// The ISO 639-3 three-letter language code
        /// </summary>
        public string ThreeLetterISOLanguageName { get; }

        public LanguageInformationAttribute(string twoLetterISOLanguageName, string threeLetterISOLanguageName)
        {
            TwoLetterISOLanguageName = twoLetterISOLanguageName;
            ThreeLetterISOLanguageName = threeLetterISOLanguageName;
        }
    }
}