using GrammarNazi.Domain.Entities.SentimAPI;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients
{
    public interface ISentimApiClient
    {
        Task<SentimResult> GetSentimentResult(string text);
    }
}