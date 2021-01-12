using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.SentimAPI;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Clients
{
    public class SentimApiClient : ISentimApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SentimApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<SentimResult> GetSentimentResult(string text)
        {
            // TODO: Get url from config
            const string url = "https://sentim-api.herokuapp.com/api/v1/";

            var body = JsonContent.Create(new SentimRequest(text));

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(url, body);

            return JsonConvert.DeserializeObject<SentimResult>(await response.Content.ReadAsStringAsync());
        }
    }
}