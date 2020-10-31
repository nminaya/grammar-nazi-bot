using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.LanguageIdentificationAPI;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace GrammarNazi.Core.Clients
{
    public class MeganingLanguageIdentificationApi : IMeganingLanguageIdentificationApi
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MeaningCloudSettings _meaningCloudSettings;

        public MeganingLanguageIdentificationApi(IHttpClientFactory httpClientFactory,
            IOptions<MeaningCloudSettings> options)
        {
            _httpClientFactory = httpClientFactory;
            _meaningCloudSettings = options.Value;
        }

        public async Task<LanguageDetectionResult> CheckLanguage(string text)
        {
            var url = $"{_meaningCloudSettings.MeaningCloudHostUrl}/key={_meaningCloudSettings.Key}&txt={HttpUtility.UrlEncode(text)}";

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(url, null);

            var result = JsonConvert.DeserializeObject<LanguageDetectionResult>(await response.Content.ReadAsStringAsync());

            return result;
        }
    }
}