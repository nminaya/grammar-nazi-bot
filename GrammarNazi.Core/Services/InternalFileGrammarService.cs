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

        public Task<CheckResult> GetCorrections(string text)
        {
            var dictionary = _fileService.GetTextFileByLine("Library/words_en-US.txt");
            var names = _fileService.GetTextFileByLine("Library/names.txt");

            var resultErros = new List<ResultError>();

            var words = text.Split(" ");

            foreach (var item in words.Where(v => !v.StartsWith("/")))
            {
                // Remove special characters
                var word = Regex.Replace(item, "[^0-9a-zA-Z:,]+", "").ToLower();

                var wordFound = dictionary.Any(v => v == word) || names.Any(v => string.Equals(v, word, StringComparison.OrdinalIgnoreCase));

                if (!wordFound)
                {
                    //TODO: Add correct word
                    resultErros.Add(new ResultError { WrongWord = item });
                }
            }

            return Task.FromResult(new CheckResult
            {
                HasErrors = resultErros.Count > 0,
                ResultErrors = resultErros
            });
        }
    }
}