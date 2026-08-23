using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System.Text;
using System.Text.RegularExpressions;

namespace GrammarNazi.Core.Services;

public class GithubService : IGithubService
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly IssueNumberCache _issueCache = new();

    private static readonly Regex CounterRegex = new(@"Exception caught counter:\s*(\d+)", RegexOptions.Compiled);

    private readonly IGitHubClient _githubClient;
    private readonly GithubSettings _githubSettings;
    private readonly ILogger<GithubService> _logger;

    public GithubService(IGitHubClient githubClient, IOptions<GithubSettings> options, ILogger<GithubService> logger)
    {
        _githubClient = githubClient;
        _githubSettings = options.Value;
        _logger = logger;
    }

    public async Task CreateBugIssue(string title, Exception exception, GithubIssueLabels githubIssueSection)
    {
        await _semaphore.WaitAsync();

        try
        {
            var issueTitle = GetTrimmedTitle(title);

            if (_issueCache.TryGet(issueTitle, out var cachedIssueNumber, out var createdOrVerifiedAtUtc))
            {
                if (await TryIncrementCounter(cachedIssueNumber, issueTitle, createdOrVerifiedAtUtc))
                {
                    return;
                }

                // Issue no longer matches open state/title on GitHub or transient 404 window expired.
                // Drop the stale entry and fall back to a remote lookup.
                _issueCache.Remove(issueTitle);
            }

            var existingIssue = await FindOpenIssueByTitle(issueTitle);

            if (existingIssue != null)
            {
                _issueCache.Set(issueTitle, existingIssue.Number);
                await UpdateCounter(existingIssue);
                return;
            }

            var createdIssue = await CreateIssue(issueTitle, exception, githubIssueSection);

            // Seeded before the semaphore is released, so a burst of identical exceptions never races
            // GitHub's eventually consistent issue list.
            _issueCache.Set(issueTitle, createdIssue.Number);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Increments the counter on a known issue number.
    /// Returns false when the issue no longer exists or is closed, so the caller can fall back to a remote lookup or fresh creation.
    /// Policy: A recurrence of an exception after its GitHub issue is closed files a new issue rather than reopening the old one.
    /// </summary>
    private async Task<bool> TryIncrementCounter(int issueNumber, string expectedTitle, DateTime createdOrVerifiedAtUtc)
    {
        try
        {
            var issue = await _githubClient.Issue.Get(_githubSettings.Username, _githubSettings.RepositoryName, issueNumber);

            bool isOpen = issue.State.StringValue != null && issue.State.Value == ItemState.Open;
            bool titleMatches = string.Equals(issue.Title, expectedTitle, StringComparison.Ordinal);
            bool hasProductionBugLabel = issue.Labels != null && issue.Labels.Any(l => l?.Name != null && string.Equals(l.Name, GithubIssueLabels.ProductionBug.Description, StringComparison.Ordinal));

            if (!isOpen || !titleMatches || !hasProductionBugLabel)
            {
                return false;
            }

            await UpdateCounter(issue);
            _issueCache.UpdateVerifiedAt(expectedTitle, DateTime.UtcNow);
            return true;
        }
        catch (NotFoundException)
        {
            var age = DateTime.UtcNow - createdOrVerifiedAtUtc;

            // GitHub's single-issue GET may lag momentarily after creation. If the cached entry was created within the last 60s,
            // treat 404 as transient to avoid filing a duplicate.
            if (age <= TimeSpan.FromSeconds(60))
            {
                _logger.LogWarning("Issue #{IssueNumber} returned 404 but was created/verified {AgeTotalSeconds:F1}s ago (within 60s grace window). Treating as transient replication lag.", issueNumber, age.TotalSeconds);
                return true;
            }

            return false;
        }
    }

    private Task UpdateCounter(Issue issue)
    {
        var issueUpdate = new IssueUpdate
        {
            Title = issue.Title,
            Body = GetBodyWithCounterUpdated(issue.Body)
        };

        return _githubClient.Issue.Update(_githubSettings.Username, _githubSettings.RepositoryName, issue.Number, issueUpdate);
    }

    private async Task<Issue> FindOpenIssueByTitle(string title)
    {
        // Note: RepositoryIssueRequest serializes filter=assigned (inherited from IssueRequest).
        // GitHub's repository-issues endpoint has no filter parameter and ignores it.
        var request = new RepositoryIssueRequest
        {
            State = ItemStateFilter.Open,
            SortProperty = IssueSort.Created,
            SortDirection = SortDirection.Descending
        };
        request.Labels.Add(GithubIssueLabels.ProductionBug.Description);

        var options = new ApiOptions { PageSize = 100 };

        var issues = await _githubClient.Issue.GetAllForRepository(
            _githubSettings.Username, _githubSettings.RepositoryName, request, options);

        return issues.FirstOrDefault(x => x.PullRequest == null && x.Title == title);
    }

    private Task<Issue> CreateIssue(string issueTitle, Exception exception, GithubIssueLabels githubIssueSection)
    {
        var newIssue = new NewIssue(issueTitle)
        {
            Body = BuildIssueBody(exception)
        };

        newIssue.Labels.Add(GithubIssueLabels.ProductionBug.Description);
        newIssue.Labels.Add(githubIssueSection.Description);

        return _githubClient.Issue.Create(_githubSettings.Username, _githubSettings.RepositoryName, newIssue);
    }

    private static string BuildIssueBody(Exception exception)
    {
        var bodyBuilder = new StringBuilder();
        bodyBuilder.Append("This is an issue created automatically by GrammarNazi when an exception was captured.\n\n");
        bodyBuilder.AppendLine($"Date (UTC): {DateTime.UtcNow}\n\n");
        bodyBuilder.AppendLine("Exception:\n\n").AppendLine(exception.ToString());
        bodyBuilder.AppendLine("\n\nException caught counter: 1.");

        return bodyBuilder.ToString();
    }

    private static string GetTrimmedTitle(string title)
    {
        if (title.Length <= Defaults.GithubIssueMaxTitleLength)
        {
            return title;
        }

        const string dots = "...";

        return title[0..(Defaults.GithubIssueMaxTitleLength - dots.Length)] + dots;
    }

    private static string GetBodyWithCounterUpdated(string issueBody)
    {
        if (string.IsNullOrEmpty(issueBody))
        {
            return "Exception caught counter: 1.";
        }

        var match = CounterRegex.Match(issueBody);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
        {
            return CounterRegex.Replace(issueBody, $"Exception caught counter: {count + 1}", 1);
        }

        return issueBody + "\n\nException caught counter: 1.";
    }

    internal static void ResetForTesting()
    {
        _semaphore.Wait();
        try
        {
            _issueCache.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Process-local map of issue title to issue number. Closes the read-after-write gap on GitHub's
    /// eventually consistent issue list endpoint: an issue created by this process is known immediately,
    /// without waiting for it to appear in a LIST response.
    /// Note: process-local. Under a multi-replica deployment each instance keeps its own map, so duplicate
    /// issues could still be created across instances.
    /// Not thread-safe: every member must be called while holding the enclosing service's semaphore.
    /// </summary>
    internal sealed class IssueNumberCache
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(6);
        private const int MaxEntries = 250;
        private const double EvictionTargetRatio = 0.8;

        private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

        public bool TryGet(string title, out int issueNumber, out DateTime createdOrVerifiedAtUtc)
        {
            RemoveExpired();

            if (_entries.TryGetValue(title, out var entry))
            {
                var now = DateTime.UtcNow;
                _entries[title] = entry with { LastAccessUtc = now };
                issueNumber = entry.Number;
                createdOrVerifiedAtUtc = entry.CreatedOrVerifiedAtUtc;
                return true;
            }

            issueNumber = default;
            createdOrVerifiedAtUtc = default;
            return false;
        }

        public void Set(string title, int issueNumber)
        {
            RemoveExpired();
            EvictOldestIfFull();

            var now = DateTime.UtcNow;
            _entries[title] = new Entry(issueNumber, now, now);
        }

        public void UpdateVerifiedAt(string title, DateTime verifiedAtUtc)
        {
            if (_entries.TryGetValue(title, out var entry))
            {
                _entries[title] = entry with { CreatedOrVerifiedAtUtc = verifiedAtUtc };
            }
        }

        public void Remove(string title) => _entries.Remove(title);

        private void RemoveExpired()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;

            var expired = _entries.Where(x => now - x.Value.LastAccessUtc > Ttl)
                                  .Select(x => x.Key)
                                  .ToList();

            foreach (var key in expired)
            {
                _entries.Remove(key);
            }
        }

        private void EvictOldestIfFull()
        {
            if (_entries.Count < MaxEntries)
            {
                return;
            }

            var target = (int)(MaxEntries * EvictionTargetRatio);

            var toEvict = _entries.OrderBy(x => x.Value.LastAccessUtc)
                                  .Take(_entries.Count - target)
                                  .Select(x => x.Key)
                                  .ToList();

            foreach (var key in toEvict)
            {
                _entries.Remove(key);
            }
        }

        // Test hooks. InternalsVisibleTo("GrammarNazi.Tests") is already declared in GrammarNazi.Core.csproj.
        internal int Count => _entries.Count;

        internal void Clear() => _entries.Clear();

        /// <summary>Inserts an entry with explicit timestamps so expiry and grace periods can be tested without waiting.</summary>
        internal void SetForTesting(string title, int issueNumber, DateTime createdOrVerifiedAtUtc, DateTime? lastAccessUtc = null)
            => _entries[title] = new Entry(issueNumber, createdOrVerifiedAtUtc, lastAccessUtc ?? createdOrVerifiedAtUtc);

        private sealed record Entry(int Number, DateTime CreatedOrVerifiedAtUtc, DateTime LastAccessUtc);
    }
}
