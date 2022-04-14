using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.YandexSpellerAPI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace GrammarNazi.Core.Clients;

public class YandexSpellerApiClient : IYandexSpellerApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<YandexSpellerApiClient> _logger;

    public YandexSpellerApiClient(IHttpClientFactory httpClientFactory,
        ILogger<YandexSpellerApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<CheckTextResponse>> CheckText(string text, string language)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("yandexSpellerApi");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/services/spellservice.json/checkText?text={HttpUtility.UrlEncode(text)}&lang={language}");

            var response = await httpClient.SendAsync(request);
            var jsonString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<IEnumerable<CheckTextResponse>>(jsonString);
        }
        catch (JsonReaderException ex)
        {
            _logger.LogWarning(ex, ex.ToString());

            // return empty result
            return Enumerable.Empty<CheckTextResponse>();
        }
    }
}