using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.LanguageToolAPI;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace GrammarNazi.Core.Clients
{
    public class LanguageToolApiClient : ILanguageToolApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LanguageToolApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<LanguageToolCheckResult> Check(string text, string languageCode)
        {
            // TODO: Get url from config
            var url = $"https://languagetool.org/api/v2/check?text={HttpUtility.UrlEncode(text)}&language={languageCode}";

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(url, null);

            return JsonConvert.DeserializeObject<LanguageToolCheckResult>(await response.Content.ReadAsStringAsync());
        }
    }
}