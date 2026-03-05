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

namespace GrammarNazi.Core.Services;

public class InternalFileGrammarService : BaseGrammarService, IGrammarService
{
    private readonly IFileService _fileService;
    private readonly IStringDiffService _stringDiffService;
    private readonly ILanguageService _languageService;

    public GrammarAlgorithms GrammarAlgorithm => GrammarAlgorithms.InternalAlgorithm;

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

            // Language not supported
            if (languageInfo == default)
            {
                return Task.FromResult(new GrammarCheckResult(default));
            }

            language = languageInfo.TwoLetterISOLanguageName;
        }
        else
        {
            language = SelectedLanguage.GetLanguageInformation().TwoLetterISOLanguageName;
        }

        var corrections = new List<GrammarCorrection>();

        var words = text.Split(" ").Where(v => !IsWhiteListWord(v)).ToArray();

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

            if (!dictionary.Contains(word))
            {
                var possibleCorrections = dictionary
                    .Where(v => Math.Abs(v.Length - word.Length) < Defaults.StringComparableRange
                        && _stringDiffService.IsInComparableRange(v, word)
                        && _stringDiffService.ComputeDistance(v, word) < Defaults.StringComparableRange)
                    .ToList();

                if (possibleCorrections.Count > 0)
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

    private HashSet<string> GetDictionaryBasedOnWords(IEnumerable<string> words, string language)
    {
        var letters = words
            .Where(v => !string.IsNullOrWhiteSpace(v) && char.IsLetter(v[0]))
            .Select(v => char.ToLower(v[0]))
            .Distinct();

        var dictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var letter in letters)
        {
            var pathNames = $"Library/Names/{letter}.txt";
            var pathLetterWords = $"Library/Dictionary/{language}/{letter}.txt";

            if (_fileService.FileExist(pathNames))
            {
                dictionary.UnionWith(_fileService.GetTextFileByLine(pathNames));
            }

            if (_fileService.FileExist(pathLetterWords))
            {
                dictionary.UnionWith(_fileService.GetTextFileByLine(pathLetterWords));
            }
        }

        return dictionary;
    }
}