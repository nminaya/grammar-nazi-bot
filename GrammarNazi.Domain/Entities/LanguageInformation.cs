namespace GrammarNazi.Domain.Entities
{
    /// <summary>
    /// ISO 639 Code Representation for a Language
    /// </summary>
    public class LanguageInformation
    {
        /// <summary>
        /// The ISO 639-2 two-letter language code
        /// </summary>
        public string TwoLetterISOLanguageName { get; init; }

        /// <summary>
        /// The ISO 639-3 three-letter language code
        /// </summary>
        public string ThreeLetterISOLanguageName { get; init; }
    }
}