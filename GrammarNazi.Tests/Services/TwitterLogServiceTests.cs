using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using NSubstitute;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.Services;

public class TwitterLogServiceTests
{
    [Fact]
    public async Task GetLastTweetId_RepositoryEmpty_Should_ReturnsZero()
    {
        // Arrange
        var repositoryMock = Substitute.For<IRepository<TwitterLog>>();

        // Returns false when Any is called
        repositoryMock.Any(Arg.Any<Expression<Func<TwitterLog, bool>>>())
            .Returns(false);

        var service = new TwitterLogService(repositoryMock);

        // Act
        var result = await service.GetLastTweetId();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetLastTweetId_RepositoryNotEmpty_Should_ReturnsMaxTweetId()
    {
        // Arrange
        var repositoryMock = Substitute.For<IRepository<TwitterLog>>();

        const long maxTweetId = 123456;

        // Returns true when Any is called
        repositoryMock.Any(Arg.Any<Expression<Func<TwitterLog, bool>>>())
            .Returns(true);

        // Returns maxTweetId value when Max is called
        repositoryMock.Max(Arg.Any<Expression<Func<TwitterLog, long>>>())
            .Returns(maxTweetId);

        var service = new TwitterLogService(repositoryMock);

        // Act
        var result = await service.GetLastTweetId();

        // Assert
        Assert.Equal(maxTweetId, result);
    }

    [Fact]
    public async Task LogTweet_TweetExistInRepository_Should_Not_AddTweet()
    {
        // Arrange
        var repositoryMock = Substitute.For<IRepository<TwitterLog>>();

        // Returns true when Any is called
        repositoryMock.Any(Arg.Any<Expression<Func<TwitterLog, bool>>>())
            .Returns(true);

        var service = new TwitterLogService(repositoryMock);

        // Act
        await service.LogTweet(123456);

        // Assert
        repositoryMock.DidNotReceive().Add(Arg.Any<TwitterLog>());
    }

    [Fact]
    public async Task LogTweet_NoTweetExistInRepository_Should_AddTweet()
    {
        // Arrange
        var repositoryMock = Substitute.For<IRepository<TwitterLog>>();

        // Returns false when Any is called
        repositoryMock.Any(Arg.Any<Expression<Func<TwitterLog, bool>>>())
            .Returns(false);

        var service = new TwitterLogService(repositoryMock);

        // Act
        await service.LogTweet(123456);

        // Assert
        repositoryMock.Received().Add(Arg.Any<TwitterLog>());
    }
}
