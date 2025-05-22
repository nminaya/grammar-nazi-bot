using Markdig;
using System.Text.RegularExpressions;

namespace GrammarNazi.Core.Utilities;

public static class StringUtils
{
    /// <summary>
    /// It will remove Twitter mentions from the given string
    /// </summary>
    /// <param name="tweetText"></param>
    /// <returns>String without mentions</returns>
    public static string RemoveMentions(string tweetText) => Regex.Replace(tweetText, @"\B@[\w\S]+", "").Trim();

    /// <summary>
    /// It will remove Twitter mentions from the given string
    /// </summary>
    /// <param name="tweetText"></param>
    /// <returns>String without mentions</returns>
    public static string RemoveHashtags(string tweetText) => Regex.Replace(tweetText, @"#[0-9a-zñA-ZÑ](\.?[0-9a-zñA-ZÑ])*", "").Trim();

    /// <summary>
    /// It will remove special characters from the given string
    /// </summary>
    /// <param name="str"></param>
    /// <returns>String without special characters</returns>
    public static string RemoveSpecialCharacters(string str) => Regex.Replace(str, "[^0-9a-zñA-ZÑÀ-ÿ]+", "");

    /// <summary>
    /// Remove emojis from a string
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string RemoveEmojis(string str)
    {
        //TODO: Simplify this regex pattern
        // Use Unicode properties to match emojis and their components.
        // \p{Extended_Pictographic} matches most emoji characters.
        // \p{Emoji_Component} matches things like skin tone modifiers, ZWJ, etc.
        // The '+' ensures that sequences of emoji characters (like flags or combined emojis) are matched together.
        const string regexPattern = @"[\p{Extended_Pictographic}\p{Emoji_Component}]+";

        return Regex.Replace(str, regexPattern, "").Trim();
    }

    /// <summary>
    /// Remove codeblocks inside blackticks "```" from a string
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string RemoveCodeBlocks(string str)
    {
        const string backticks = "```";

        var matches = Regex.Matches(str, backticks);

        while (matches.Count > 1)
        {
            var startIndex = matches[0].Index;
            var endIndex = matches[1].Index + backticks.Length;
            var codeBlock = str[startIndex..endIndex];

            str = str.Replace(codeBlock, "");

            matches = Regex.Matches(str, backticks);
        }

        return str;
    }

    /// <summary>
    /// Converts Markdown to plain text
    /// </summary>
    /// <param name="text"></param>
    public static string MarkDownToPlainText(string text)
    {
        // config to avoid new lines (\n) in parsed text
        var markDownConfig = new MarkdownPipelineBuilder()
                                    .ConfigureNewLine("")
                                    .Build();

        return Markdown.ToPlainText(text, markDownConfig);
    }
}