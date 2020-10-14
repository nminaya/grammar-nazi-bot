using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class InternalFileGrammarService : BaseGrammarService, IGrammarService
    {
        private readonly IFileService _fileService;
        private readonly IStringDiffService _stringDiffService;
        private readonly ILanguageService _languageService;

        public GrammarAlgorithms GrammarAlgorith => GrammarAlgorithms.InternalAlgorithm;

        public InternalFileGrammarService(IFileService fileService,
            IStringDiffService stringDiffService,
            ILanguageService languageService)
        {
            _fileService = fileService;
            _stringDiffService = stringDiffService;
            _languageService = languageService;
        }

        public Task<GrammarCheckResult> GetCorrections(string text)
        {
            string language;

            if (SelectedLanguage == SupportedLanguages.Auto)
            {
                var languageInfo = _languageService.IdentifyLanguage(text);

                language = languageInfo.TwoLetterISOLanguageName;
            }
            else
            {
                language = LanguageUtils.GetLanguageCode(SelectedLanguage.GetDescription());
            }

            var corrections = new List<GrammarCorrection>();

            var words = text.Split(" ");

            foreach (var item in words)
            {
                if (string.IsNullOrWhiteSpace(item) || !char.IsLetter(item[0]))
                {
                    continue;
                }

                // Remove special characters
                var word = StringUtils.RemoveSpecialCharacters(item).ToLower();

                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                var names = _fileService.GetTextFileByLine($"Library/Names/{word.First()}.txt");

                var dictionaryAndNames = _fileService.GetTextFileByLine($"Library/Dictionary/{language}/{word.First()}.txt").Union(names);

                var wordFound = dictionaryAndNames.Contains(word);

                if (!wordFound)
                {
                    var possibleCorrections = dictionaryAndNames.Where(v => _stringDiffService.IsInComparableRange(v, word) && _stringDiffService.ComputeDistance(v, word) < Defaults.StringComparableRange);

                    if (possibleCorrections.Any())
                    {
                        var correction = new GrammarCorrection
                        {
                            WrongWord = item,
                            PossibleReplacements = possibleCorrections,
                            Message = $"The word \"{item}\" doesn't exist or isn't in the internal dictionary."
                        };
                        corrections.Add(correction);
                    }
                }
            }

            return Task.FromResult(new GrammarCheckResult(corrections));
        }
    }
}