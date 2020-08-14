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
        public async Task<LanguageToolCheckResult> Check(string text)
        {
            // TODO: Get url from config
            var url = $"https://languagetool.org/api/v2/check?text={HttpUtility.UrlEncode(text)}&language=en-US";

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(url, null);

            var result = JsonConvert.DeserializeObject<LanguageToolCheckResult>(await response.Content.ReadAsStringAsync());

            return result;
        }
    }
}