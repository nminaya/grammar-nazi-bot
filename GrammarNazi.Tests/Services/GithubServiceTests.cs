using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Octokit;
using System.Net;
using System.Reflection;
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
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings
        {
            Username = "test-user",
            RepositoryName = "test-repo"
        };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

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
    public async Task CreateBugIssue_RemoteLookup_Should_RequestAllPages()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var issueTitle = "Test Issue Page 2";
        var issueMock = CreateMockIssue(105, issueTitle, "Exception caught counter: 1.");

        ApiOptions passedOptions = null;

        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Do<ApiOptions>(opt => passedOptions = opt))
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue> { issueMock }));

        // Act
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: ApiOptions specifies PageSize = 100 and PageCount == null (all pages)
        Assert.NotNull(passedOptions);
        Assert.Equal(100, passedOptions.PageSize);
        Assert.Null(passedOptions.PageCount);

        await githubClientMock.Issue.DidNotReceive().Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.Received(1).Update(githubSettings.Username, githubSettings.RepositoryName, 105, Arg.Any<IssueUpdate>());
    }

    [Fact]
    public async Task CreateBugIssue_CacheHit_RefreshesLruTimestamp()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var issueTitle = "Hot Issue";
        var issueMock = CreateMockIssue(1, issueTitle, "Exception caught counter: 1.");

        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromResult(issueMock));

        // Seed cache entry created 5 hours ago
        GetCache().SetForTesting(issueTitle, 1, DateTime.UtcNow.AddHours(-5));

        // Act 1: Hit cache
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Act 2: Hit cache again
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: Issue.Get called twice, Issue.Create NEVER called
        await githubClientMock.Issue.DidNotReceive().Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.Received(2).Get(githubSettings.Username, githubSettings.RepositoryName, 1);
    }

    [Fact]
    public async Task CreateBugIssue_Cumulative404sPast60sFromCreation_Should_InvalidateCacheAndCreateReplacementIssue()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var issueTitle = "Recurring Exception Issue";
        var newReplacementIssue = CreateMockIssue(2, issueTitle, "Exception caught counter: 1.");

        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromException<Issue>(new NotFoundException("Hard deleted", HttpStatusCode.NotFound)));

        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(newReplacementIssue));

        // Plant an entry created 65 seconds ago
        var creationTime = DateTime.UtcNow.AddSeconds(-65);
        GetCache().SetForTesting(issueTitle, 1, creationTime);

        // Act: Exception recurs
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: Age > 60s past creation/verification causes cache invalidation and replacement issue creation
        await githubClientMock.Issue.Received(1).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
    }

    [Fact]
    public async Task CreateBugIssue_CachedIssueIsClosed_Should_CreateNewIssue()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var issueTitle = "Closed Issue Title";
        var closedIssueMock = CreateMockIssue(1, issueTitle, "Exception caught counter: 1.", ItemState.Closed);
        var newIssueMock = CreateMockIssue(2, issueTitle, "Exception caught counter: 1.");

        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromResult(closedIssueMock));

        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(newIssueMock));

        GetCache().SetForTesting(issueTitle, 1, DateTime.UtcNow);

        // Act
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: Creates new issue 2, closed issue 1 NOT updated
        await githubClientMock.Issue.Received(1).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.DidNotReceive().Update(githubSettings.Username, githubSettings.RepositoryName, 1, Arg.Any<IssueUpdate>());
    }

    [Fact]
    public async Task CreateBugIssue_CachedIssueWasRenamed_Should_CreateNewIssue()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var originalTitle = "Title A";
        var renamedIssueMock = CreateMockIssue(1, "Title B Renamed", "Exception caught counter: 1.");
        var newIssueMock = CreateMockIssue(2, originalTitle, "Exception caught counter: 1.");

        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromResult(renamedIssueMock));

        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(newIssueMock));

        GetCache().SetForTesting(originalTitle, 1, DateTime.UtcNow);

        // Act
        await githubService.CreateBugIssue(originalTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: Creates new issue for Title A, does NOT update renamed issue 1
        await githubClientMock.Issue.Received(1).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.DidNotReceive().Update(githubSettings.Username, githubSettings.RepositoryName, 1, Arg.Any<IssueUpdate>());
    }

    [Fact]
    public async Task CreateBugIssue_CachedIssueMissingProductionBugLabel_Should_CreateNewIssue()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var issueTitle = "Unlabeled Issue";
        var unlabelledIssueMock = CreateMockIssue(1, issueTitle, "Exception caught counter: 1.", ItemState.Open, labels: ["documentation"]);
        var newIssueMock = CreateMockIssue(2, issueTitle, "Exception caught counter: 1.");

        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromResult(unlabelledIssueMock));

        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(newIssueMock));

        GetCache().SetForTesting(issueTitle, 1, DateTime.UtcNow);

        // Act
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: Creates new issue for Title, does NOT update issue 1
        await githubClientMock.Issue.Received(1).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.DidNotReceive().Update(githubSettings.Username, githubSettings.RepositoryName, 1, Arg.Any<IssueUpdate>());
    }

    [Fact]
    public async Task CreateBugIssue_Fresh404Within60s_Should_TreatAsTransientAndNotCreateDuplicate()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var issueTitle = "Freshly Created Issue";

        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromException<Issue>(new NotFoundException("Lagging indexing", HttpStatusCode.NotFound)));

        GetCache().SetForTesting(issueTitle, 1, DateTime.UtcNow.AddSeconds(-10));

        // Act
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: Treated as transient, NO Issue.Create and NO Issue.GetAllForRepository called!
        await githubClientMock.Issue.DidNotReceive().Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
        await githubClientMock.Issue.DidNotReceive().GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Any<ApiOptions>());
    }

    [Fact]
    public async Task CreateBugIssue_Older404Past60s_Should_FallThroughAndCreate()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var issueTitle = "Old Deleted Issue";
        var newIssueMock = CreateMockIssue(2, issueTitle, "body");

        githubClientMock.Issue.Get(githubSettings.Username, githubSettings.RepositoryName, 1)
            .Returns(Task.FromException<Issue>(new NotFoundException("Truly deleted", HttpStatusCode.NotFound)));

        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(newIssueMock));

        GetCache().SetForTesting(issueTitle, 1, DateTime.UtcNow.AddSeconds(-120));

        // Act
        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Assert: Falls through to remote lookup and creates new issue
        await githubClientMock.Issue.Received(1).Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>());
    }

    [Fact]
    public async Task CreateBugIssue_ConcurrentCalls_Should_CreateOneAndUpdateOthers()
    {
        // Arrange
        var githubClientMock = Substitute.For<IGitHubClient>();
        var optionsMock = Substitute.For<IOptions<GithubSettings>>();
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings
        {
            Username = "test-user",
            RepositoryName = "test-repo"
        };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var issueTitle = "Test Issue Concurrent";
        var exception = new Exception("Test Exception");
        var label = GithubIssueLabels.Telegram;

        var issueMock = CreateMockIssue(1, issueTitle, "Exception caught counter: 1.");

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
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings
        {
            Username = "test-user",
            RepositoryName = "test-repo"
        };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

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
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings
        {
            Username = "test-user",
            RepositoryName = "test-repo"
        };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

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
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

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
        var loggerMock = Substitute.For<ILogger<GithubService>>();
        var githubSettings = new GithubSettings { Username = "u", RepositoryName = "r" };
        optionsMock.Value.Returns(githubSettings);

        var githubService = new GithubService(githubClientMock, optionsMock, loggerMock);

        var issueTitle = "Deleted Issue Title";
        var issueMock = CreateMockIssue(1, issueTitle, "Exception caught counter: 1.");

        // First creation
        githubClientMock.Issue.GetAllForRepository(
            githubSettings.Username, githubSettings.RepositoryName, Arg.Any<RepositoryIssueRequest>(), Arg.Any<ApiOptions>())
            .Returns(Task.FromResult((IReadOnlyList<Issue>)new List<Issue>()));

        githubClientMock.Issue.Create(githubSettings.Username, githubSettings.RepositoryName, Arg.Any<NewIssue>())
            .Returns(Task.FromResult(issueMock));

        await githubService.CreateBugIssue(issueTitle, new Exception(), GithubIssueLabels.Telegram);

        // Next call: Issue.Get throws NotFoundException (hard-deleted on GitHub, entry created >60s ago)
        GetCache().SetForTesting(issueTitle, 1, DateTime.UtcNow.AddSeconds(-120));

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

    private static Issue CreateMockIssue(int number, string title, string body, ItemState state = ItemState.Open, IEnumerable<string> labels = null)
    {
        var issue = (Issue)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Issue));
        typeof(Issue).GetProperty("Number").SetValue(issue, number);
        typeof(Issue).GetProperty("Title").SetValue(issue, title);
        typeof(Issue).GetProperty("Body").SetValue(issue, body);
        typeof(Issue).GetProperty("State").SetValue(issue, new StringEnum<ItemState>(state));

        var labelList = (labels ?? [GithubIssueLabels.ProductionBug.Description])
            .Select(l =>
            {
                var lbl = (Label)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Label));
                typeof(Label).GetProperty("Name").SetValue(lbl, l);
                return lbl;
            })
            .ToList();

        typeof(Issue).GetProperty("Labels").SetValue(issue, labelList);

        return issue;
    }
}
