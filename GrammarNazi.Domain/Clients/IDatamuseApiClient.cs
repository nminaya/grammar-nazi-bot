using GrammarNazi.Domain.Entities.DatamuseAPI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients
{
    public interface IDatamuseApiClient
    {
        Task<WordCheckResult> CheckWord(string word, string language);
    }
}