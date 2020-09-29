using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Core.Services
{
    public class BaseGrammarService
    {
        protected SupportedLanguages SelectedLanguage;
        protected CorrectionStrictnessLevels SelectedStrictnessLevel;

        public void SetSelectedLanguage(SupportedLanguages supportedLanguage)
        {
            SelectedLanguage = supportedLanguage;
        }

        public void SetStrictnessLevel(CorrectionStrictnessLevels correctionStrictnessLevel)
        {
            SelectedStrictnessLevel = correctionStrictnessLevel;
        }
    }
}