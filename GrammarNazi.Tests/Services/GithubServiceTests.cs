using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.Services
{
    public class GithubServiceTests
    {
        [Fact]
        public async Task CreateBugIssue_MultipleCalls_ShouldBatchAndCorrectCount()
        {
            // Arrange
            var githubClientMock = Substitute.For<IGitHubClient>();
            var optionsMock = Substitute.For<IOptions<GithubSettings>>();
            var loggerMock = Substitute.For<ILogger<GithubService>>();

            var settings = new GithubSettings
            {
                Username = "test-user",
                RepositoryName = "test-repo"
            };
            optionsMock.Value.Returns(settings);

            var service = new GithubService(githubClientMock, optionsMock, loggerMock);

            var existingIssue = Substitute.For<Issue>();
            // Use reflection to set the Title and Body since they might be read-only
            typeof(Issue).GetProperty("Number")?.SetValue(existingIssue, 1);
            typeof(Issue).GetProperty("Title")?.SetValue(existingIssue, "Application Exception: Test Exception");
            typeof(Issue).GetProperty("Body")?.SetValue(existingIssue, "... Exception caught counter: 1.");

            githubClientMock.Issue.GetAllForRepository(settings.Username, settings.RepositoryName, Arg.Any<RepositoryIssueRequest>())
                .Returns(new List<Issue> { existingIssue });

            // Start the background worker
            var cts = new CancellationTokenSource();
            _ = service.StartAsync(cts.Token);

            // Act
            // Call CreateBugIssue multiple times
            for (int i = 0; i < 5; i++)
            {
                await service.CreateBugIssue("Application Exception: Test Exception", new Exception("Test Exception"), GithubIssueLabels.Telegram);
            }

            // Give some time for background processing
            await Task.Delay(2000);

            // Assert
            await githubClientMock.Issue.Received().Update(settings.Username, settings.RepositoryName, existingIssue.Number, Arg.Is<IssueUpdate>(u => u.Body.Contains("Exception caught counter: 6.")));

            // Cleanup
            await service.StopAsync(new CancellationToken());
            cts.Cancel();
        }
    }
}