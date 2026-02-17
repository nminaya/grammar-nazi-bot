using GrammarNazi.Core.Utilities;
using Xunit;

namespace GrammarNazi.Tests.Utilities;

public class SqlExceptionHelperTests
{
    [Theory]
    [InlineData(53, "any message")]
    [InlineData(258, "any message")]
    [InlineData(10054, "any message")]
    [InlineData(10060, "any message")]
    [InlineData(4060, "any message")]
    [InlineData(10928, "any message")]
    [InlineData(10929, "any message")]
    [InlineData(40197, "any message")]
    [InlineData(40501, "any message")]
    [InlineData(40613, "any message")]
    [InlineData(0, "A network-related or instance-specific error occurred")]
    [InlineData(0, "TCP Provider")]
    [InlineData(0, "error: 40")]
    [InlineData(0, "Could not open a connection to SQL Server")]
    [InlineData(0, "server was not found or was not accessible")]
    public void IsTransient_TransientError_Should_ReturnTrue(int number, string message)
    {
        // Act
        var result = SqlExceptionHelper.IsTransient(number, message);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(123, "Fatal error")]
    [InlineData(0, "Some other error")]
    public void IsTransient_NonTransientError_Should_ReturnFalse(int number, string message)
    {
        // Act
        var result = SqlExceptionHelper.IsTransient(number, message);

        // Assert
        Assert.False(result);
    }
}
