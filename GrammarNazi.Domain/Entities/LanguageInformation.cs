namespace GrammarNazi.Domain.Entities
{
    /// <summary>
    /// ISO 639 Code Representation for a Language
    /// </summary>
    public class LanguageInformation
    {
        /// <summary>
        /// The ISO 639-2 two-letter code for the language
        /// </summary>
        public string TwoLetterISOLanguageName { get; set; }

        /// <summary>
        /// The ISO 639-3 three-letter code for the language
        /// </summary>
        public string ThreeLetterISOLanguageName { get; set; }
    }
}