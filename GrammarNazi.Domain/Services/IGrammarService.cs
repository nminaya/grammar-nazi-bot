using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services;

public interface IGrammarService
{
    public GrammarAlgorithms GrammarAlgorith { get; }

    void SetSelectedLanguage(SupportedLanguages supportedLanguage);

    void SetStrictnessLevel(CorrectionStrictnessLevels correctionStrictnessLevel);

    void SetWhiteListWords(IEnumerable<string> whiteListWords);

    Task<GrammarCheckResult> GetCorrections(string text);
}
