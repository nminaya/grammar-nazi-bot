using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients;

public interface IGroqApiClient
{
    Task<string> GetChatCompletion(string systemPrompt, string userPrompt);
}
