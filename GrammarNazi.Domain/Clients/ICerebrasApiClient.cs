
namespace GrammarNazi.Domain.Clients;

public interface ICerebrasApiClient
{
    Task<string> GetChatCompletion(string systemPrompt, string userPrompt);
}
