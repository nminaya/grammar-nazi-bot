using GrammarNazi.Domain.Entities.YandexSpellerAPI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Clients
{
    public interface IYandexSpellerApiClient
    {
        Task<IEnumerable<CheckTextResponse>> CheckText(string text, string language);
    }
}