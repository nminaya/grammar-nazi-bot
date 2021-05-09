using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.MeaningCloudAPI;
using GrammarNazi.Domain.Entities.MeaningCloudAPI.SentimentAnalysis;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;

namespace GrammarNazi.Core.Clients
{
    public class MeaningCloudSentimentAnalysisApiClient : IMeaningCloudSentimentAnalysisApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MeaningCloudSettings _meaningCloudSettings;

        public MeaningCloudSentimentAnalysisApiClient(IHttpClientFactory httpClientFactory,
            IOptions<MeaningCloudSettings> options)
        {
            _httpClientFactory = httpClientFactory;
            _meaningCloudSettings = options.Value;
        }

        public async Task<SentimentAnalysisResult> GetSentimentResult(string text, string language)
        {
            var url = $"{_meaningCloudSettings.MeaningCloudSentimentHostUrl}?key={_meaningCloudSettings.Key}&txt={HttpUtility.UrlEncode(text)}&lang={language}";

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(url);
            var responseJson = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<SentimentAnalysisResult>(responseJson);
        }
    }
}