using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.LanguageIdentificationAPI;
using GrammarNazi.Domain.Entities.LanguageIdentificationAPI.SentimentAnalysis;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

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
            var requestBody = new SentimentAnalysisRequest
            {
                Key = _meaningCloudSettings.Key,
                Lang = language,
                Text = text
            };

            var body = JsonContent.Create(requestBody);

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(_meaningCloudSettings.MeaningCloudSentimentHostUrl, body);

            return JsonConvert.DeserializeObject<SentimentAnalysisResult>(await response.Content.ReadAsStringAsync());
        }
    }
}