using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.LanguageToolAPI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace GrammarNazi.Core.Clients
{
    public class LanguageToolApiClient : ILanguageToolApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LanguageToolApiClient> _logger;

        public LanguageToolApiClient(IHttpClientFactory httpClientFactory,
            ILogger<LanguageToolApiClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<LanguageToolCheckResult> Check(string text, string languageCode)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("languageToolApi");

                var request = new HttpRequestMessage(HttpMethod.Post, $"api/v2/check?text={HttpUtility.UrlEncode(text)}&language={languageCode}");

                var response = await httpClient.SendAsync(request);

                return JsonConvert.DeserializeObject<LanguageToolCheckResult>(await response.Content.ReadAsStringAsync());
            }
            catch (JsonReaderException ex)
            {
                _logger.LogWarning(ex, ex.ToString());

                // return empty result
                return new()
                {
                    Matches = new(),
                    Language = new() { Code = Defaults.LanguageCode }
                };
            }
        }
    }
}