using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class InternalFileGrammarService : IGrammarService
    {
        private readonly IFileService _fileService;
        private readonly IStringDiffService _stringDiffService;

        public InternalFileGrammarService(IFileService fileService, IStringDiffService stringDiffService)
        {
            _fileService = fileService;
            _stringDiffService = stringDiffService;
        }

        public Task<GrammarCheckResult> GetCorrections(string text)
        {
            var dictionary = _fileService.GetTextFileByLine("Library/words_en-US.txt");
            var names = _fileService.GetTextFileByLine("Library/names.txt");

            var corrections = new List<GrammarCorrection>();

            var words = text.Split(" ");

            foreach (var item in words)
            {
                // Remove special characters
                var word = Regex.Replace(item, "[^0-9a-zA-Z:,]+", "").ToLower();

                var wordFound = dictionary.Any(v => v == word);
                var nameFound = names.Any(v => string.Equals(v, word, StringComparison.OrdinalIgnoreCase));

                if (!wordFound && !nameFound)
                {
                    if (!wordFound)
                    {
                        var possibleCorrections = dictionary.Where(v => _stringDiffService.IsInComparableRange(v, word) && _stringDiffService.ComputeDistance(v, word) < 2);
                        corrections.Add(new GrammarCorrection { WrongWord = item, PossibleReplacements = possibleCorrections });
                        continue;
                    }

                    if (!nameFound)
                    {
                        var possibleCorrections = names.Where(v => _stringDiffService.IsInComparableRange(v, word) && _stringDiffService.ComputeDistance(v, word) < 2);
                        corrections.Add(new GrammarCorrection { WrongWord = item, PossibleReplacements = possibleCorrections });
                    }
                }
            }

            return Task.FromResult(new GrammarCheckResult(corrections));
        }
    }
}