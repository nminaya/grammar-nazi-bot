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

        public Task<CheckResult> GetCorrections(string text)
        {
            const string dictionaryFilePath = "Library/words_en-US.txt";

            var words = text.Split(" ").Select(v => v.ToLower());
            var dictionary = _fileService.GetTextFileByLine(dictionaryFilePath);

            var resultErros = new List<ResultError>();

            foreach (var item in words.Where(v => !v.StartsWith("/")))
            {
                // Remove special characters
                var word = Regex.Replace(item, "[^0-9a-zA-Z:,]+", "");

                var wordFound = dictionary.Any(v => v == word);

                if (!wordFound)
                {
                    //TODO: Add correct word
                    resultErros.Add(new ResultError { CorrectWord = "", WrongWord = word });
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