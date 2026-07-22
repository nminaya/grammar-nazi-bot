using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using LiteDB;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Xunit;
using GrammarNazi.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace GrammarNazi.Tests.Services
{
    public class CatchExceptionServiceTests
    {
        public CatchExceptionServiceTests()
        {
            // Clear static state before each test
            var field = typeof(CatchExceptionService).GetField("SqlRateLimitStates", BindingFlags.NonPublic | BindingFlags.Static);
            var dictionary = (System.Collections.IDictionary)field.GetValue(null);
            dictionary.Clear();
        }

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
        public void HandleException_ExternalApiUnavailableException_Should_LogWarning()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();
            var service = new CatchExceptionService(githubServiceMock, loggerMock);
            var exception = new ExternalApiUnavailableException("External API service unavailable");

            // Act
            service.HandleException(exception, GithubIssueLabels.ProductionBug);

            // Assert
            var numberOfCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Warning));

            Assert.Equal(1, numberOfCalls);
            githubServiceMock.DidNotReceive().CreateBugIssue(Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<GithubIssueLabels>());
        }

        [Fact]
        public void HandleException_TaskCanceledExceptionWithInnerTimeoutException_Should_LogWarning()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();
            var service = new CatchExceptionService(githubServiceMock, loggerMock);
            var timeoutException = new TimeoutException("The operation timed out.");
            var exception = new TaskCanceledException("Task canceled due to timeout", timeoutException);

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
        public void HandleException_TaskCanceledExceptionWithoutInnerException_Should_LogErrorAndCreateBugIssue()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();
            var service = new CatchExceptionService(githubServiceMock, loggerMock);
            var exception = new TaskCanceledException("Task canceled normally");

            // Act
            service.HandleException(exception, GithubIssueLabels.Telegram);

            // Assert
            var numberOfCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Error));

            Assert.Equal(1, numberOfCalls);
            githubServiceMock.Received().CreateBugIssue("Application Exception: Task canceled normally", exception, GithubIssueLabels.Telegram);
        }

        [Fact]
        public void HandleException_GroqRateLimitException_Should_LogWarning()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();
            var service = new CatchExceptionService(githubServiceMock, loggerMock);
            var exception = new GroqRateLimitException("Groq rate limit reached");

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

        [Theory]
        [InlineData(53, "Transient error")]
        [InlineData(0, "TCP Provider error")]
        public async Task HandleException_TransientSqlException_Should_LogWarningAndRateLimit(int number, string message)
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock, loggerMock);

            var exception = CreateSqlException(number, message);

            // Act
            // Call multiple times to test rate limiting (burst-only)
            for (int i = 0; i < 15; i++)
            {
                service.HandleException(exception, GithubIssueLabels.Telegram);
            }

            // Assert

            // LogWarning should be called for every occurrence (15 times)
            var warningCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Warning));

            Assert.Equal(15, warningCalls);

            // CreateBugIssue should be called:
            // 1. When burst reaches 10 (at occurrence #10)
            // Occurrences 11-15 accumulate but don't reach 10 again.
            // Total: 1 call
            await githubServiceMock.Received(1).CreateBugIssue(Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<GithubIssueLabels>());
        }

        [Fact]
        public void HandleException_SingleTransientSqlException_Should_LogWarningOnly_And_NotCreateIssue()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock, loggerMock);

            var exception = CreateSqlException(53, "Transient error");

            // Act
            service.HandleException(exception, GithubIssueLabels.Telegram);

            // Assert
            var warningCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Warning));

            Assert.Equal(1, warningCalls);

            githubServiceMock.DidNotReceive().CreateBugIssue(Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<GithubIssueLabels>());
        }

        [Fact]
        public async Task HandleException_TransientSqlException_BurstThreshold_Should_CreateIssueAtTenthOccurrence()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock, loggerMock);

            var exception = CreateSqlException(53, "Transient error");

            // Act & Assert
            // First 9 occurrences should not create a Bug Issue
            for (int i = 0; i < 9; i++)
            {
                service.HandleException(exception, GithubIssueLabels.Telegram);
            }

            _ = githubServiceMock.DidNotReceive().CreateBugIssue(Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<GithubIssueLabels>());

            // The 10th occurrence should trigger exactly one Bug Issue
            service.HandleException(exception, GithubIssueLabels.Telegram);

            await githubServiceMock.Received(1).CreateBugIssue(Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<GithubIssueLabels>());
        }

        [Fact]
        public void HandleException_NonTransientSqlException_Should_LogErrorAndCreateBugIssue()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CatchExceptionService>>();
            var githubServiceMock = Substitute.For<IGithubService>();

            var service = new CatchExceptionService(githubServiceMock, loggerMock);

            var exception = CreateSqlException(123, "Fatal SQL error");

            // Act
            service.HandleException(exception, GithubIssueLabels.Telegram);

            // Assert
            var errorCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Error));

            Assert.Equal(1, errorCalls);

            githubServiceMock.Received().CreateBugIssue("Application Exception: Fatal SQL error", exception, GithubIssueLabels.Telegram);
        }

        private SqlException CreateSqlException(int number, string message)
        {
            var collection = (SqlErrorCollection)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SqlErrorCollection));
            var error = (SqlError)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SqlError));

            typeof(SqlError).GetField("_number", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(error, number);
            typeof(SqlError).GetField("_message", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(error, message);

            var list = new List<object> { error };
            typeof(SqlErrorCollection).GetField("_errors", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(collection, list);

            var exception = (SqlException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SqlException));
            typeof(SqlException).GetField("_errors", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(exception, collection);
            typeof(Exception).GetField("_message", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(exception, message);

            return exception;
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