using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Services;
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
            var dictionaryAndNames = dictionary.Union(names.Select(v => v.ToLower()));

            var corrections = new List<GrammarCorrection>();

            var words = text.Split(" ");

            foreach (var item in words)
            {
                // Remove special characters
                var word = Regex.Replace(item, "[^0-9a-zA-Z:,]+", "").ToLower();

                var wordFound = dictionaryAndNames.Any(v => v == word);

                if (!wordFound)
                {
                    var possibleCorrections = dictionaryAndNames.Where(v => _stringDiffService.IsInComparableRange(v, word) && _stringDiffService.ComputeDistance(v, word) < 2);

                    if (possibleCorrections.Any())
                    {
                        corrections.Add(new GrammarCorrection { WrongWord = item, PossibleReplacements = possibleCorrections });
                    }
                }
            }

            return Task.FromResult(new GrammarCheckResult(corrections));
        }
    }
}