﻿using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class DatamuseApiService : BaseGrammarService, IGrammarService
    {
        public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.DatamuseApi;

        private readonly IDatamuseApiClient _datamuseApiClient;
        private readonly ILanguageService _languageService;

        public DatamuseApiService(IDatamuseApiClient datamuseApiClient,
            ILanguageService languageService)
        {
            _datamuseApiClient = datamuseApiClient;
            _languageService = languageService;
        }

        public async Task<GrammarCheckResult> GetCorrections(string text)
        {
            string language;

            if (SelectedLanguage == SupportedLanguages.Auto)
            {
                var languageInfo = _languageService.IdentifyLanguage(text);

                if (languageInfo == default)
                {
                    return new(default);
                }

                language = languageInfo.TwoLetterISOLanguageName;
            }
            else
            {
                language = LanguageUtils.GetLanguageCode(SelectedLanguage.GetDescription());
            }

            var words = text.Split(" ")
                .Select(StringUtils.RemoveSpecialCharacters)
                .Where(v => !char.IsNumber(v[0]) && !IsWhiteListWord(v));

            var wordsCheckTasks = words.Select(v => _datamuseApiClient.CheckWord(v, language));

            var corrections = new List<GrammarCorrection>();

            foreach (var wordCheckResultTask in wordsCheckTasks)
            {
                var wordCheckResult = await wordCheckResultTask;

                if (wordCheckResult.HasCorrections)
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