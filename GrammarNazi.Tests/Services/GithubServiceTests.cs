using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.Services;

[Collection("GithubServiceTests")]
public class GithubServiceTests
{
    public GithubServiceTests()
    {
        GithubService.ResetForTesting();
    }

    [Fact]
    public async Task CreateBugIssue_StaleRead_CalledTwiceSameTitle_Should_CreateOnceAndUpdateOnce()
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

        var issueTitle = "Test Issue Stale Read";
        var exception = new Exception("Test Exception");
        var label = GithubIssueLabels.Telegram;

        // Mock GetAllForRepository to always return empty list (simulating GitHub's stale read / delay in indexing)
        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username,
            githubSettings.RepositoryName,
            Arg.Any<RepositoryIssueRequest>(),
            Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        // Mock Issue.Create
        var createdIssueMock = CreateMockIssue(1, issueTitle, "Exception caught counter: 1.");
        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(createdIssueMock));

        // Mock Issue.Get (used on cache hit)
        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromResult(createdIssueMock));

        // Act - First call creates the issue and populates process-local cache
        await githubService.CreateBugIssue(issueTitle, exception, label);

        // Act - Second call with same title, even with stale list response, should hit cache
        await githubService.CreateBugIssue(issueTitle, exception, label);

        // Assert
        await githubClientMock.Issue.Received(1).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.Received(1).Update(githubSettings.Username, githubSettings.RepositoryName, 1, Arg.Any<IssueUpdate>());
    }

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

        var issueTitle = "Test Issue Concurrent";
        var exception = new Exception("Test Exception");
        var label = GithubIssueLabels.Telegram;

        var issueMock = CreateMockIssue(1, issueTitle, "Exception caught counter: 1.");

        // Stale list endpoint always returns empty list
        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username,
            githubSettings.RepositoryName,
            Arg.Any<RepositoryIssueRequest>(),
            Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(issueMock));

        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromResult(issueMock));

        // Act - 10 concurrent calls
        var tasks = Enumerable.Range(0, 10).Select(_ => githubService.CreateBugIssue(issueTitle, exception, label));
        await Task.WhenAll(tasks);

        // Assert
        await githubClientMock.Issue.Received(1).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.Received(9).Update(githubSettings.Username, githubSettings.RepositoryName, 1, Arg.Any<IssueUpdate>());
    }

    [Fact]
    public async Task CreateBugIssue_DifferentTitles_Should_CreateSeparateIssues()
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

        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username,
            githubSettings.RepositoryName,
            Arg.Any<RepositoryIssueRequest>(),
            Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        int idCounter = 1;
        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(x =>
            {
                var newIssue = x.Arg<NewIssue>();
                return Task.FromResult(CreateMockIssue(idCounter++, newIssue.Title, newIssue.Body));
            });

        // Act
        await githubService.CreateBugIssue("Title A", new Exception(), GithubIssueLabels.Telegram);
        await githubService.CreateBugIssue("Title B", new Exception(), GithubIssueLabels.Telegram);

        // Assert
        await githubClientMock.Issue.Received(2).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
    }

    [Fact]
    public async Task CreateBugIssue_CacheExpired_Should_PerformRemoteLookupAndUpdate()
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

        var issueTitle = "Test Issue Expiry";
        var issueMock = CreateMockIssue(1, issueTitle, "Exception caught counter: 1.");

        int getAllCalls = 0;
        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username,
            githubSettings.RepositoryName,
            Arg.Any<RepositoryIssueRequest>(),
            Arg.Any<ApiOptions>())
            .Returns(_ =>
            {
                if (getAllCalls == 0)
                {
                    getAllCalls++;
                    return Task.FromResult((IReadOnlyList<Issue>)new List<Issue>());
                }
                return Task.FromResult((IReadOnlyList<Issue>)new List<Issue> { issueMock });
            });

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(issueMock));

        // Act 1: Initial call creates issue and populates cache
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Plant pre-expired entry using cache test hook
        GetCache().SetForTesting(issueTitle, 1, DateTime.UtcNow.AddHours(-7));

        // Act 2: Call after TTL expiry
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: Remote lookup found existing issue and updated it (not created a second one)
        await githubClientMock.Issue.Received(1).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.Received(1).Update(githubSettings.Username, githubSettings.RepositoryName, 1, Arg.Any<IssueUpdate>());
    }

    [Fact]
    public async Task CreateBugIssue_CacheExceedsCap_Should_EvictOldestEntries()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock);

        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        int id = 1;
        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(x => Task.FromResult(CreateMockIssue(id++, x.Arg<NewIssue>().Title, "body")));

        // Act: Push 260 distinct issue titles
        for (int i = 0; i < 260; i++)
        {
            await githubService.CreateBugIssue($"Title {i}", new Exception(), GithubIssueLabels.Telegram);
        }

        // Assert: Cache count is capped below MaxCacheEntries (250)
        Assert.True(GetCache().Count <= 250);
    }

    [Fact]
    public async Task CreateBugIssue_NotFoundExceptionOnCacheHit_Should_InvalidateAndCreateNew()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock);

        var issueTitle = "Deleted Issue Title";
        var issueMock = CreateMockIssue(1, issueTitle, "Exception caught counter: 1.");

        // First creation
        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(issueMock));

        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Next call: Issue.Get throws NotFoundException (hard-deleted on GitHub)
        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromException<Issue>(new NotFoundException("Not found", HttpStatusCode.NotFound)));

        // Act
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: Create was called twice (once initially, once after cache eviction on NotFoundException)
        await githubClientMock.Issue.Received(2).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
    }

    [Theory]
    [InlineData("Exception caught counter: 5.", "Exception caught counter: 6.")]
    [InlineData("Some text\n\nException caught counter: 1.", "Some text\n\nException caught counter: 2.")]
    [InlineData("No counter present", "No counter present\n\nException caught counter: 1.")]
    [InlineData("Exception caught counter: invalid", "Exception caught counter: invalid\n\nException caught counter: 1.")]
    [InlineData("Exception caught counter: 10.\nTrailing comment from maintainer", "Exception caught counter: 11.\nTrailing comment from maintainer")]
    public void GetBodyWithCounterUpdated_Scenarios(string initialBody, string expectedUpdatedBody)
    {
        // Arrange
        var method = typeof(GithubService).GetMethod("GetBodyWithCounterUpdated", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = (string)method.Invoke(null, new object[] { initialBody });

        // Assert
        Assert.Equal(expectedUpdatedBody, result);
    }

    private static GithubService.IssueNumberCache GetCache()
    {
        var cacheField = typeof(GithubService).GetField("_issueCache", BindingFlags.NonPublic | BindingFlags.Static);
        return (GithubService.IssueNumberCache)cacheField.GetValue(null);
    }

    private static Issue CreateMockIssue(int number, string title, string body)
    {
        var issue = (Issue)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Issue));
        typeof(Issue).GetProperty("Number").SetValue(issue, number);
        typeof(Issue).GetProperty("Title").SetValue(issue, title);
        typeof(Issue).GetProperty("Body").SetValue(issue, body);
        return issue;
    }
}
