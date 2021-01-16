using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.LanguageToolAPI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
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
                // TODO: Get url from config
                var url = $"https://languagetool.org/api/v2/check?text={HttpUtility.UrlEncode(text)}&language={languageCode}";

                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync(url, null);

                return JsonConvert.DeserializeObject<LanguageToolCheckResult>(await response.Content.ReadAsStringAsync());
            }
            catch (JsonReaderException ex)
            {
                _logger.LogWarning(ex, ex.ToString());

                // return empty result
                return new()
                {
                    Matches = new(),
                    Language = new() { Code = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName }
                };
            }
        }
    }
}