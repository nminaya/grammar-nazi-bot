using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.Services;

public class GithubServiceTests
{
    [Fact]
    public async Task CreateBugIssue_ConcurrentCalls_Should_CreateOneAndUpdateOthers()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var githubSettings = new GithubSettings
        {
            Username = "test-user",
            RepositoryName = "test-repo"
        };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock);

        var issueTitle = "Test Issue";
        var exception = new Exception("Test Exception");
        var label = GithubIssueLabels.Telegram;

        int getAllCalls = 0;
        var issuesList = new List<Issue>();

        githubClientMock.Issue.GetAllForRepository(githubSettings.Username, githubSettings.RepositoryName)
            .Returns(_ =>
            {
                var currentIssues = issuesList.ToList();
                if (getAllCalls == 0)
                {
                    getAllCalls++;
                    return Task.FromResult((IReadOnlyList<Issue>)new List<Issue>());
                }
                return Task.FromResult((IReadOnlyList<Issue>)currentIssues);
            });

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(x =>
            {
                var newIssue = x.Arg<NewIssue>();
                var issue = (Issue)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Issue));
                typeof(Issue).GetProperty("Title").SetValue(issue, newIssue.Title);
                typeof(Issue).GetProperty("Number").SetValue(issue, 1);
                typeof(Issue).GetProperty("Body").SetValue(issue, newIssue.Body);
                issuesList.Add(issue);
                return Task.FromResult(issue);
            });

        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ => githubService.CreateBugIssue(issueTitle, exception, label));
        await Task.WhenAll(tasks);

        // Assert
        await githubClientMock.Issue.Received(1).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.Received(9).Update(githubSettings.Username, githubSettings.RepositoryName, 1, Arg.Any<IssueUpdate>());
    }
}
