using GrammarNazi.Domain.Entities.LanguageIdentificationAPI;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients
{
    public interface IMeganingLanguageIdentificationApi
    {
        Task<LanguageDetectionResult> CheckLanguage(string text);
    }
}