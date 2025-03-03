using GrammarNazi.Domain.Entities.GeminiAPI;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients;

public interface IGeminiApiClient
{
    Task<GenerateContentResponse> GenerateContent(string promt);
}
