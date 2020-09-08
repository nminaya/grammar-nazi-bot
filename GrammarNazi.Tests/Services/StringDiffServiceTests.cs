using GrammarNazi.Core.Services;
using Xunit;

namespace GrammarNazi.Tests.Services
{
    public class StringDiffServiceTests
    {
        [Theory]
        [InlineData("TEST", "TEST", 0)]
        [InlineData("TEST", "BEST", 1)]
        [InlineData("TEST", "TETS", 2)]
        [InlineData("TEST", "TTES", 2)]
        [InlineData("TEST", "TEST1", 1)]
        public void ComputeDistance_GivenTwoStrings_Should_ReturnExpectedResult(string a, string b, int expectedResult)
        {
            // Arrange
            var service = new StringDiffService();

            // Act
            var result = service.ComputeDistance(a, b);

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}