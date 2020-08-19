using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface IGrammarService
    {
        public GrammarAlgorithms GrammarAlgorith { get; }

        Task<GrammarCheckResult> GetCorrections(string text);
    }
}