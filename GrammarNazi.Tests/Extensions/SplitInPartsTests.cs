using GrammarNazi.Core.Extensions;
using System;
using System.Linq;
using Xunit;

namespace GrammarNazi.Tests.Extensions
{
    public class SplitInPartsTests
    {
        [Fact]
        public void NullString_Should_ThrowsArgumentNullException()
        {
            // Arrange
            const string str = null;

            // Act => Assert
            Assert.Throws<ArgumentNullException>(() => str.SplitInParts(1).ToList());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(-500)]
        [InlineData(-12958415)]
        public void NegativeParts_Should_ThrowsArgumentException(int partSize)
        {
            // Arrange
            const string str = "Test";

            // Act => Assert
            Assert.Throws<ArgumentException>(() => str.SplitInParts(partSize).ToList());
        }

        [Theory]
        [InlineData("111", new string[] { "1", "1", "1" }, 1)]
        [InlineData("12121212", new string[] { "12", "12", "12", "12" }, 2)]
        [InlineData("12121212", new string[] { "121", "212", "12" }, 3)]
        [InlineData("This is a test", new string[] { "This ", "is a ", "test" }, 5)]
        public void GivenStringAndPartSize_Should_SplitStringAsExpected(string actual, string[] expected, int partSize)
        {
            var result = actual.SplitInParts(partSize).ToList();

            Assert.Equal(expected, result);
        }
    }
}