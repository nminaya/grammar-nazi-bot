using GrammarNazi.Domain.Entities.YandexSpellerAPI;

namespace GrammarNazi.Domain.Clients;

public interface IYandexSpellerApiClient
{
    Task<IEnumerable<CheckTextResponse>> CheckText(string text, string language);
}