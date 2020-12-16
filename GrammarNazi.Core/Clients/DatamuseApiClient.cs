using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.DatamuseAPI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace GrammarNazi.Core.Clients
{
    public class DatamuseApiClient : IDatamuseApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DatamuseApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<WordCheck>> CheckWord(string word, string language)
        {
            var url = $"https://api.datamuse.com/words?={HttpUtility.UrlEncode(word)}&v={language}";

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(url);

            return JsonConvert.DeserializeObject<IEnumerable<WordCheck>>(await response.Content.ReadAsStringAsync());
        }
    }
}