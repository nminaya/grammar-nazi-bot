using System.Text.RegularExpressions;

namespace GrammarNazi.Core.Utilities
{
    public static class StringUtils
    {
        /// <summary>
        /// It will remove Twitter mentions from the given string
        /// </summary>
        /// <param name="tweetText"></param>
        /// <returns>String without mentions</returns>
        public static string RemoveMentions(string tweetText) => Regex.Replace(tweetText, @"@[0-9a-zA-Z](\.?[0-9a-zA-Z])*", "").Trim();

        /// <summary>
        /// It will remove special characters from the given string
        /// </summary>
        /// <param name="str"></param>
        /// <returns>String without special characters</returns>
        public static string RemoveSpecialCharacters(string str) => Regex.Replace(str, "[^0-9a-zA-Z]+", "");
    }
}