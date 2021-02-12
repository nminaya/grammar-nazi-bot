using GrammarNazi.Domain.Enums;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities
{
    /// <summary>
    /// Bot Configuration for Discord Channel
    /// </summary>
    public class DiscordChannelConfig
    {
        /// <summary>
        /// Channel Id
        /// </summary>
        public ulong ChannelId { get; set; }

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
        /// Server Id
        /// </summary>
        public ulong Guild { get; set; }

        /// <summary>
        /// List of ignored words
        /// </summary>
        public List<string> WhiteListWords { get; set; } = new();

        public override bool Equals(object obj)
        {
            if (obj is DiscordChannelConfig channelConfig)
                return ChannelId == channelConfig.ChannelId;

            return false;
        }

        public override int GetHashCode()
        {
            return ChannelId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ChannelId}";
        }
    }
}