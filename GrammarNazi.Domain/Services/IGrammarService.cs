using GrammarNazi.Domain.Entities;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface IGrammarService
    {
        public GrammarAlgorithms GrammarAlgorith { get; }
        Task<GrammarCheckResult> GetCorrections(string text);
    }
}