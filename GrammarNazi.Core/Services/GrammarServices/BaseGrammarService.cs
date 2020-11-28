using GrammarNazi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GrammarNazi.Core.Services
{
    public abstract class BaseGrammarService
    {
        protected SupportedLanguages SelectedLanguage;
        protected CorrectionStrictnessLevels SelectedStrictnessLevel;
        protected List<string> WhiteListWords = new();

        public void SetSelectedLanguage(SupportedLanguages supportedLanguage)
        {
            SelectedLanguage = supportedLanguage;
        }

        public void SetStrictnessLevel(CorrectionStrictnessLevels correctionStrictnessLevel)
        {
            SelectedStrictnessLevel = correctionStrictnessLevel;
        }

        public void SetWhiteListWords(IEnumerable<string> whiteListWords)
        {
            WhiteListWords.Clear();
            WhiteListWords.AddRange(whiteListWords);
        }

        protected bool IsWhiteListWord(string word)
        {
            return WhiteListWords.Any(w => w.Contains(word, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}