using GrammarNazi.Core.Utilities;
using Xunit;

namespace GrammarNazi.Tests.Utilities
{
    public class LanguageUtilsTests
    {
        [Theory]
        [InlineData("eng", "en")]
        [InlineData("spa", "es")]
        public void GetLanguageCode_Should_ReturnExpectedResult(string actual, string expected)
        {
            // Arrange > Act
            var result = LanguageUtils.GetLanguageCode(actual);

            // Assert 
            Assert.Equal(expected, result);
        }
    }
}
