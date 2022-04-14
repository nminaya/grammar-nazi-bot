using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GrammarNazi.Core.Services;

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
        WhiteListWords.RemoveAll(_ => true);

        if (whiteListWords?.Any() == true)
        {
            WhiteListWords.AddRange(whiteListWords);
        }
    }

    protected bool IsWhiteListWord(string word)
    {
        return WhiteListWords.Any(w => w.Contains(word, StringComparison.InvariantCultureIgnoreCase));
    }

    protected static string GetCorrectionMessage(string word, string language)
    {
        if (language == SupportedLanguages.English.GetLanguageInformation().TwoLetterISOLanguageName)
            return $"The word \"{word}\" doesn't exist or isn't in the dictionary.";

        return $"La palabra \"{word}\" no existe o no está en el diccionario.";
    }
}