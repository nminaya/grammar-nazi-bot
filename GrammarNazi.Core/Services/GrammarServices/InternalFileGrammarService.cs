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

                if (languageInfo == default)
                {
                    return Task.FromResult(new GrammarCheckResult(default));
                }

                language = languageInfo.TwoLetterISOLanguageName;
            }
            else
            {
                language = LanguageUtils.GetLanguageCode(SelectedLanguage.GetDescription());
            }

            var corrections = new List<GrammarCorrection>();

            var words = text.Split(" ").Where(v => !IsWhiteListWord(v));

            var dictionary = GetDictionaryBasedOnWords(words, language);

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

                var wordFound = dictionary.Contains(word);

                if (!wordFound)
                {
                    var possibleCorrections = dictionary.Where(v => _stringDiffService.IsInComparableRange(v, word) && _stringDiffService.ComputeDistance(v, word) < Defaults.StringComparableRange);

                    if (possibleCorrections.Any())
                    {
                        var correction = new GrammarCorrection
                        {
                            WrongWord = item,
                            PossibleReplacements = possibleCorrections,
                            Message = GetCorrectionMessage(item, language)
                        };
                        corrections.Add(correction);
                    }
                }
            }

            return Task.FromResult(new GrammarCheckResult(corrections));
        }

        private IReadOnlyList<string> GetDictionaryBasedOnWords(IEnumerable<string> words, string language)
        {
            var letters = words
                .Where(v => !string.IsNullOrWhiteSpace(v) && char.IsLetter(v[0]))
                .Select(v => v.ToLower()[0])
                .Distinct();

            var dictionary = new List<string>();

            foreach (var letter in letters)
            {
                var pathNames = $"Library/Names/{letter}.txt";
                var pathLetterWords = $"Library/Dictionary/{language}/{letter}.txt";

                var names = _fileService.FileExist(pathNames) ? _fileService.GetTextFileByLine(pathNames) : Enumerable.Empty<string>();
                var letterWords = _fileService.FileExist(pathLetterWords) ? _fileService.GetTextFileByLine(pathLetterWords) : Enumerable.Empty<string>();

                dictionary.AddRange(names);
                dictionary.AddRange(letterWords);
            }

            return dictionary;
        }
    }
}