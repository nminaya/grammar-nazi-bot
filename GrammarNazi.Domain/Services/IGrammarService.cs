using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface IGrammarService
    {
        public GrammarAlgorithms GrammarAlgorith { get; }

        void SetSelectedLanguage(SupportedLanguages supportedLanguage);

        void SetStrictnessLevel(CorrectionStrictnessLevels correctionStrictnessLevel);

        Task<GrammarCheckResult> GetCorrections(string text);
    }
}