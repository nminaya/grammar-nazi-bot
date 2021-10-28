using GrammarNazi.Domain.Enums;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities
{
    /// <summary>
    /// Bot Configuration for a Chat
    /// </summary>
    public class ChatConfiguration
    {
        /// <summary>
        /// Backing field for WhitelistWords
        /// </summary>
        private List<string> _whiteListWords = new();

        /// <summary>
        /// Unique identifier for the Chat
        /// </summary>
        public long ChatId { get; set; }

        /// <summary>
        /// Selected GrammarAlgorithm
        /// </summary>
        public GrammarAlgorithms GrammarAlgorithm { get; set; }

        /// <summary>
        /// Selected SupportedLanguages
        /// </summary>
        public SupportedLanguages SelectedLanguage { get; set; }

        /// <summary>
        /// Selected CorrectionStrictnessLevels
        /// </summary>
        public CorrectionStrictnessLevels CorrectionStrictnessLevel { get; set; } = CorrectionStrictnessLevels.Intolerant;

        /// <summary>
        /// True if bot stopped
        /// </summary>
        public bool IsBotStopped { get; set; }

        /// <summary>
        /// True if correction details hidden
        /// </summary>
        public bool HideCorrectionDetails { get; set; }

        /// <summary>
        /// List of ignored words
        /// </summary>
        public List<string> WhiteListWords
        {
            get => _whiteListWords;
            set => _whiteListWords = value ?? new();
        }

        public override bool Equals(object obj)
        {
            if (obj is ChatConfiguration chatConfiguration)
                return ChatId == chatConfiguration.ChatId;

            return false;
        }

        public override int GetHashCode()
        {
            return ChatId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ChatId}";
        }
    }
}