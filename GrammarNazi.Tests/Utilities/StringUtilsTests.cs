using GrammarNazi.Core.Utilities;
using Xunit;

namespace GrammarNazi.Tests.Utilities;

public class StringUtilsTests
{
    [Theory]
    [InlineData("Test", "Test")]
    [InlineData("Test's", "Tests")]
    [InlineData("Ñoño's", "Ñoños")]
    [InlineData("!@#$%^&*[]_", "")]
    [InlineData("papá", "papá")]
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
    [InlineData("#ñ1 #ht2 this is a test #Ñ123", "this is a test")]
    public void RemoveHashtags_GivenString_Should_RemoveHashtags_And_ReturnsExpectedResult(string actual, string expected)
    {
        // Arrange > Act
        var result = StringUtils.RemoveHashtags(actual);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("bored🔫😡😆😡❤️😡😡❤️", "bored")]
    [InlineData("testé🔫😡😆😡❤️😡😡❤️", "testé")]
    [InlineData("é🔫😡á😆é😡❤️😡ú😡❤️ ó", "éáéú ó")]
    [InlineData("🔫😡Don't😆😡❤️😡😡❤️", "Don't")]
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

    [Fact]
    public void RemoveCodeBlocks_GivenStringWithSingleCodeBlock_Should_RemoveCodeBlock()
    {
        const string test = @"```cs
                                private void Test() => Console.WriteLine(""Method param"");
                                ```
                                This is a test";

        var result = StringUtils.RemoveCodeBlocks(test);

        Assert.DoesNotContain("private void Test() => Console.WriteLine", result);
        Assert.Contains("This is a test", result);
    }

    [Fact]
    public void RemoveCodeBlocks_GivenStringWithMultipleCodeBlocks_Should_RemoveAllCodeBlocks()
    {
        const string test = @"```cs
                                private void Test() => Console.WriteLine(""Method param"");
                                ```
                                This is a test1
                                ```javascript
                                function test() {
                                 console.log(""Method param"");
                                }
                                ```
                                This is a test2
                                ";

        var result = StringUtils.RemoveCodeBlocks(test);

        Assert.DoesNotContain("private void Test() => Console.WriteLine", result);
        Assert.DoesNotContain("console.log", result);
        Assert.Contains("This is a test", result);
        Assert.Contains("This is a test2", result);
    }

    [Theory]
    [InlineData("This is a **test**", "This is a test")]
    [InlineData("This is a _test_", "This is a test")]
    [InlineData("**This** is *a* _test_", "This is a test")]
    public void MarkDownToPlainText_GivenString_Should_ReturnsExpectedResult(string actual, string expected)
    {
        // Arrange > Act
        var result = StringUtils.MarkDownToPlainText(actual);

        // Assert
        Assert.Equal(expected, result);
    }
}
