using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Entities
{
    public class ChatConfiguration
    {
        public long ChatId { get; set; }

        public GrammarAlgorithms GrammarAlgorithm { get; set; }

        public SupportedLanguages SelectedLanguage { get; set; }
    }
}