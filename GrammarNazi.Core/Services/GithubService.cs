using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("GrammarNazi.Tests")]

namespace GrammarNazi.Core.Services;

public class GithubService : IGithubService
{
    private static readonly System.Threading.SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Process-local cache for GitHub issue titles to numbers.
    /// Note: This cache is process-local. If the application is deployed in a multi-replica setup,
    /// duplicate issues may still occur across different instances.
    /// </summary>
    private static readonly Dictionary<string, CachedIssue> _issueCache = new(StringComparer.Ordinal);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);
    private const int MaxCacheEntries = 250;

    private sealed record CachedIssue(int Number, DateTime CachedAtUtc);

    private static readonly Regex CounterRegex = new(@"Exception caught counter:\s*(\d+)", RegexOptions.Compiled);

    private readonly IGitHubClient _githubClient;
    private readonly GithubSettings _githubSettings;

    public GithubService(IGitHubClient githubClient, IOptions<GithubSettings> options)
    {
        _githubClient = githubClient;
        _githubSettings = options.Value;
    }

    public async Task CreateBugIssue(string title, Exception exception, GithubIssueLabels githubIssueSection)
    {
        await _semaphore.WaitAsync();

        try
        {
            var now = DateTime.UtcNow;

            // Time-based sweep on every call
            if (_issueCache.Count > 0)
            {
                var expired = _issueCache.Where(kvp => now - kvp.Value.CachedAtUtc > CacheTtl)
                                         .Select(kvp => kvp.Key)
                                         .ToList();
                foreach (var key in expired) _issueCache.Remove(key);
            }

            // Hard size cap
            if (_issueCache.Count >= MaxCacheEntries)
            {
                var itemsToEvict = _issueCache
                    .OrderBy(kvp => kvp.Value.CachedAtUtc)
                    .Take(_issueCache.Count - (int)(MaxCacheEntries * 0.8))
                    .Select(kvp => kvp.Key)
                    .ToList();
                foreach (var key in itemsToEvict) _issueCache.Remove(key);
            }

            var issueTitle = GetTrimmedTitle(title);

            // Cache lookup
            if (_issueCache.TryGetValue(issueTitle, out var cachedIssue))
            {
                try
                {
                    var existingIssue = await _githubClient.Issue.Get(_githubSettings.Username, _githubSettings.RepositoryName, cachedIssue.Number);
                    var issueUpdate = new IssueUpdate
                    {
                        Title = existingIssue.Title,
                        Body = GetBodyWithCounterUpdated(existingIssue.Body)
                    };

                    await _githubClient.Issue.Update(_githubSettings.Username, _githubSettings.RepositoryName, existingIssue.Number, issueUpdate);
                    return;
                }
                catch (NotFoundException)
                {
                    _issueCache.Remove(issueTitle);
                }
            }

            // Remote lookup (cache miss)
            var issue = await GetIssueByTittle(issueTitle);

            if (issue != null)
            {
                _issueCache[issueTitle] = new CachedIssue(issue.Number, now);

                var issueUpdate = new IssueUpdate
                {
                    Title = issue.Title,
                    Body = GetBodyWithCounterUpdated(issue.Body)
                };

                await _githubClient.Issue.Update(_githubSettings.Username, _githubSettings.RepositoryName, issue.Number, issueUpdate);
                return;
            }

            var bodyBuilder = new StringBuilder();
            bodyBuilder.Append("This is an issue created automatically by GrammarNazi when an exception was captured.\n\n");
            bodyBuilder.AppendLine($"Date (UTC): {now}\n\n");
            bodyBuilder.AppendLine("Exception:\n\n").AppendLine(exception.ToString());
            bodyBuilder.AppendLine("\n\nException caught counter: 1.");

            var newIssue = new NewIssue(issueTitle)
            {
                Body = bodyBuilder.ToString()
            };
            newIssue.Labels.Add(GithubIssueLabels.ProductionBug.GetDescription());
            newIssue.Labels.Add(githubIssueSection.GetDescription());

            var createdIssue = await _githubClient.Issue.Create(_githubSettings.Username, _githubSettings.RepositoryName, newIssue);
            _issueCache[issueTitle] = new CachedIssue(createdIssue.Number, now);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal static void ResetForTesting()
    {
        _issueCache.Clear();
    }

    private async Task<Issue> GetIssueByTittle(string title)
    {
        var request = new RepositoryIssueRequest
        {
            State = ItemStateFilter.Open,
            SortProperty = IssueSort.Created,
            SortDirection = SortDirection.Descending
        };
        request.Labels.Add(GithubIssueLabels.ProductionBug.GetDescription());

        var options = new ApiOptions { PageSize = 100, PageCount = 1 };

        var issues = await _githubClient.Issue.GetAllForRepository(
            _githubSettings.Username, _githubSettings.RepositoryName, request, options);

        return issues.FirstOrDefault(v => v.PullRequest == null && v.Title == title);
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
}
