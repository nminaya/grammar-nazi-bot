using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.YandexSpellerAPI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace GrammarNazi.Core.Clients
{
    public class YandexSpellerApiClient : IYandexSpellerApiClient
    {
        public async Task<IEnumerable<CheckTextResponse>> CheckText(string text, string language)
        {
            // TODO: Get url from config
            var url = $"https://speller.yandex.net/services/spellservice.json/checkText?text={HttpUtility.UrlEncode(text)}&lang={language}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<IEnumerable<CheckTextResponse>>(jsonString);

            return result;
        }
    }
}