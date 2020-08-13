using GrammarNazi.Domain.Entities;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface IGrammarService
    {
        Task<CheckResult> GetCorrections(string text);
    }
}