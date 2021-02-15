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
            const string regextPattern = "(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff]|[^\u0000-\u007F]+)";

            return Regex.Replace(str, regextPattern, "").Trim();
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
    }
}