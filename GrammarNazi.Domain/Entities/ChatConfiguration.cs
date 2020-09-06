using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Entities
{
    public class ChatConfiguration
    {
        public long ChatId { get; set; }

        public GrammarAlgorithms GrammarAlgorithm { get; set; }

        public SupportedLanguages SelectedLanguage { get; set; }

        public bool IsBotStopped { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ChatConfiguration chatConfiguration)
                return ChatId == chatConfiguration.ChatId;

            return base.Equals(obj);
        }
    }
}