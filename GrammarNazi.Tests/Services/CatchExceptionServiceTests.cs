using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using LiteDB;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Xunit;
using GrammarNazi.Domain.Exceptions;

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
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock, loggerMock);

            var exception = new ApiRequestException(exceptionMessage);

            // Act
            service.HandleException(exception, GithubIssueLabels.Telegram);

            // Assert

            // Verify LogWarning was called
            var numberOfCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Warning));

            Assert.Equal(1, numberOfCalls);

            // Verify CreateBugIssue was never called
            githubServiceMock.DidNotReceive().CreateBugIssue(Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<GithubIssueLabels>());
        }

        [Fact]
        public void HandleException_GeminiServiceUnavailableException_Should_LogWarning()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();
            var service = new CatchExceptionService(githubServiceMock, loggerMock);
            var exception = new GeminiServiceUnavailableException("Gemini service unavailable");

            // Act
            service.HandleException(exception, GithubIssueLabels.ProductionBug);

            // Assert
            var numberOfCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Warning));

            Assert.Equal(1, numberOfCalls);
            githubServiceMock.DidNotReceive().CreateBugIssue(Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<GithubIssueLabels>());
        }

        [Theory]
        [InlineData("Bot API Request timed out")]
        public void HandleException_RequestException_Timeout_Should_LogWarning(string exceptionMessage)
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock, loggerMock);

            var exception = new RequestException(exceptionMessage);

            // Act
            service.HandleException(exception, GithubIssueLabels.Telegram);

            // Assert
            var numberOfCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Warning));

            Assert.Equal(1, numberOfCalls);

            githubServiceMock.DidNotReceive().CreateBugIssue(Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<GithubIssueLabels>());
        }

        [Fact]
        public void HandleException_RequestException_TaskCanceled_Should_LogWarning()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock, loggerMock);

            var innerException = new TaskCanceledException("A task was canceled.");
            var exception = new RequestException("Request canceled", innerException);

            // Act
            service.HandleException(exception, GithubIssueLabels.Telegram);

            // Assert
            var numberOfCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Warning));

            Assert.Equal(1, numberOfCalls);

            githubServiceMock.DidNotReceive().CreateBugIssue(Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<GithubIssueLabels>());
        }

        [Fact]
        public void HandleError_ExceptionCaptured_Should_LogErrorAndCreateBugIssue()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock, loggerMock);

            var exception = new Exception("Fatal test exception");

            // Act
            service.HandleException(exception, GithubIssueLabels.Telegram);

            // Assert

            var numberOfCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Error));

            Assert.Equal(1, numberOfCalls);

            // Verify CreateBugIssue was called
            githubServiceMock.Received().CreateBugIssue("Application Exception: Fatal test exception", exception, GithubIssueLabels.Telegram);
        }
    }
}