using GrammarNazi.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace GrammarNazi.Tests.Utilities
{
    public class StringUtilsTests
    {
        [Theory]
        [InlineData("Test", "Test")]
        [InlineData("Test's","Tests")]
        [InlineData("!@#$%^&*[]_", "")]
        public void RemoveSpecialCharacters_GivenString_Should_ReturnsExpectedResult(string actual, string expected)
        {
            // Arrange > Act
            var result = StringUtils.RemoveSpecialCharacters(actual);

            // Assert 
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("@userTest this is a test", "this is a test")]
        [InlineData("@USERTTEST this is a test", "this is a test")]
        [InlineData("this is a test @USERTTEST", "this is a test")]
        [InlineData("@USERTTEST1 @USERTTEST2 this is a test @USERTTEST", "this is a test")]
        [InlineData("@_USERTTEST1 @USERT_TEST2 this is a test @__USE-RTTEST", "this is a test")]
        public void RemoveMentions_GivenString_Should_RemoveMentions_And_ReturnsExpectedResult(string actual, string expected)
        {
            // Arrange > Act
            var result = StringUtils.RemoveMentions(actual);

            // Assert 
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("#hashTag this is a test", "this is a test")]
        [InlineData("#HASHTAG this is a test", "this is a test")]
        [InlineData("this is a test #HashTaG", "this is a test")]
        [InlineData("#Ht1 #ht2 this is a test #HTT33", "this is a test")]
        public void RemoveHashtags_GivenString_Should_RemoveHashtags_And_ReturnsExpectedResult(string actual, string expected)
        {
            // Arrange > Act
            var result = StringUtils.RemoveHashtags(actual);

            // Assert 
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("This is fun 😂", "This is fun")]
        [InlineData("😀😁😂🤣😃😄😅😆💋👏😜💖😢😎🎶😉😍😒😘🤞😊😩😬👍", "")]
        [InlineData("Test😁Test1", "TestTest1")]
        [InlineData("😁Test😁Test1😁", "TestTest1")]
        public void RemoveEmojis_GivenString_Should_RemoveEmojis_And_ReturnsExpectedResult(string actual, string expected)
        {
            // Arrange > Act
            var result = StringUtils.RemoveEmojis(actual);

            // Assert 
            Assert.Equal(expected, result);
        }
    }
}
