using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.MeaningCloudAPI;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace GrammarNazi.Core.Clients
{
    public class MeganingCloudLangApiClient : IMeganingCloudLangApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MeaningCloudSettings _meaningCloudSettings;

        public MeganingCloudLangApiClient(IHttpClientFactory httpClientFactory,
            IOptions<MeaningCloudSettings> options)
        {
            _httpClientFactory = httpClientFactory;
            _meaningCloudSettings = options.Value;
        }

        public async Task<LanguageDetectionResult> GetLanguage(string text)
        {
            var httpClient = _httpClientFactory.CreateClient("meaninCloudLanguageApi");
            var request = new HttpRequestMessage(HttpMethod.Get, $"?key={_meaningCloudSettings.Key}&txt={HttpUtility.UrlEncode(text)}");

            var response = await httpClient.SendAsync(request);

            return JsonConvert.DeserializeObject<LanguageDetectionResult>(await response.Content.ReadAsStringAsync());
        }
    }
}