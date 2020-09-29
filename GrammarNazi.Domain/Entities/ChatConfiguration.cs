using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Entities
{
    public class ChatConfiguration
    {
        public long ChatId { get; set; }

        public GrammarAlgorithms GrammarAlgorithm { get; set; }

        public SupportedLanguages SelectedLanguage { get; set; }

        public CorrectionStrictnessLevels CorrectionStrictnessLevels { get; set; } = CorrectionStrictnessLevels.Tolerant;

        public bool IsBotStopped { get; set; }

        public bool HideCorrectionDetails { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ChatConfiguration chatConfiguration)
                return ChatId == chatConfiguration.ChatId;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ChatId.GetHashCode();
        }
    }
}