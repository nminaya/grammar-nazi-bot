namespace GrammarNazi.Domain.Entities.YandexSpellerAPI;

/// <summary>
/// YandexSpeller error codes https://yandex.ru/dev/speller/doc/dg/reference/error-codes.html
/// </summary>
public enum YandexSpellerErrorCodes
{
    /// <summary>
    /// The word is not in the dictionary.
    /// </summary>
    UnknownWord = 1,

    /// <summary>
    /// Repeat a word.
    /// </summary>
    RepeatWord = 2,

    /// <summary>
    /// Incorrect use of uppercase and lowercase letters.
    /// </summary>
    Capitalization = 3
}
