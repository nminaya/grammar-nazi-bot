using GrammarNazi.Core.Services;
using Xunit;

namespace GrammarNazi.Tests.Services;

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

    [Theory]
    [InlineData("TEST", "TEST", true)]
    [InlineData("TEST", "BEST", true)]
    [InlineData("TEST", "REST", true)]
    [InlineData("TEST", "LEFT", false)]
    [InlineData("TEST", "CHEST", false)]
    [InlineData("TEST", "DATA", false)]
    public void IsInComparableRange_GivenTwoStrings_Should_ReturnExpectedResult(string a, string b, bool expectedResult)
    {
        // Arrange
        var service = new StringDiffService();

        // Act
        var result = service.IsInComparableRange(a, b);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
