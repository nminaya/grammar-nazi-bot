using GrammarNazi.Domain.Entities.LanguageIdentificationAPI;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients
{
    public interface IMeganingCloudLangApiClient
    {
        Task<LanguageDetectionResult> GetLanguage(string text);
    }
}