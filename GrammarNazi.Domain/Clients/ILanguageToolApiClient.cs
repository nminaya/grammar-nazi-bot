using GrammarNazi.Domain.Entities.LanguageToolAPI;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients;

public interface ILanguageToolApiClient
{
    Task<LanguageToolCheckResult> Check(string text, string languageCode);
}