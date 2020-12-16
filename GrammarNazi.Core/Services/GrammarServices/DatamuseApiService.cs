using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class DatamuseApiService : BaseGrammarService, IGrammarService
    {
        public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.DatamuseApi;

        private readonly IDatamuseApiClient _datamuseApiClient;

        public DatamuseApiService(IDatamuseApiClient datamuseApiClient)
        {
            _datamuseApiClient = datamuseApiClient;
        }

        public async Task<GrammarCheckResult> GetCorrections(string text)
        {
            //TODO: Validate language
            var language = "";

            var words = text.Split(" ").Where(v => !IsWhiteListWord(v));

            var wordsCheckTasks = words.Select(v => _datamuseApiClient.CheckWord(v, "en"));

            var corrections = new List<GrammarCorrection>();

            foreach (var wordCheckResultTask in wordsCheckTasks)
            {
                var wordCheckResult = await wordCheckResultTask;

                if (wordCheckResult.Words.Any() && wordCheckResult.Words.All(v => !v.Word.Equals(wordCheckResult.Word, StringComparison.OrdinalIgnoreCase)))
                {
                    corrections.Add(new()
                    {
                        WrongWord = wordCheckResult.Word,
                        PossibleReplacements = wordCheckResult.Words.Select(v => v.Word),
                        Message = GetCorrectionMessage(wordCheckResult.Word, language)
                    });
                }
            }

            return new(corrections);
        }
    }
}