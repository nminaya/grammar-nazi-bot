using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Telegram.Bot.Exceptions;
using Xunit;

namespace GrammarNazi.Tests.Services
{
    public class CatchExceptionServiceTests
    {
        [Theory]
        [InlineData("bot was blocked by the user")]
        [InlineData("bot was kicked from the supergroup")]
        [InlineData("have no rights to send a message")]
        public void HandleException_ApiRequestException_Should_LogWarning(string exceptionMessage)
        {
            // Arrange
            var loggerMock = new Mock<ILogger<CatchExceptionService>>();
            var githubServiceMock = new Mock<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock.Object, loggerMock.Object);

            var exception = new ApiRequestException(exceptionMessage);

            // Act
            service.HandleException(exception, GithubIssueLabels.Telegram);

            // Assert

            // Verify LogWarning was called
            loggerMock.Verify(x => x.Log(
                            LogLevel.Warning,
                            It.IsAny<EventId>(),
                            It.IsAny<It.IsAnyType>(),
                            It.IsAny<Exception>(),
                            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            // Verify CreateBugIssue was never called
            githubServiceMock.Verify(x => x.CreateBugIssue(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<GithubIssueLabels>()), Times.Never);
        }

        [Fact]
        public void HandleError_ExceptionCaptured_Should_LogErrorAndCreateBugIssue()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<CatchExceptionService>>();
            var githubServiceMock = new Mock<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock.Object, loggerMock.Object);

            var exception = new Exception("Fatal test exception");

            // Act
            service.HandleException(exception, GithubIssueLabels.Telegram);

            // Assert

            // Verify LogError was called
            loggerMock.Verify(x => x.Log(
                            LogLevel.Error,
                            It.IsAny<EventId>(),
                            It.IsAny<It.IsAnyType>(),
                            exception,
                            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            // Verify CreateBugIssue was called
            githubServiceMock.Verify(x => x.CreateBugIssue("Application Exception: Fatal test exception", exception, GithubIssueLabels.Telegram));
        }
    }
}