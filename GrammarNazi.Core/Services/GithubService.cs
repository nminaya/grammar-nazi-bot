using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services;

public class GithubService : BackgroundService, IGithubService
{
    private readonly IGitHubClient _githubClient;
    private readonly GithubSettings _githubSettings;
    private readonly ILogger<GithubService> _logger;
    private readonly Channel<IssueRequest> _issueChannel;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _issueSemaphores = new();

    public GithubService(IGitHubClient githubClient, IOptions<GithubSettings> options, ILogger<GithubService> logger)
    {
        _githubClient = githubClient;
        _githubSettings = options.Value;
        _logger = logger;
        _issueChannel = Channel.CreateUnbounded<IssueRequest>();
    }

    public async Task CreateBugIssue(string title, Exception exception, GithubIssueLabels githubIssueSection)
    {
        await _issueChannel.Writer.WriteAsync(new IssueRequest(title, exception, githubIssueSection));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GithubService background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await _issueChannel.Reader.WaitToReadAsync(stoppingToken))
                {
                    var requests = new List<IssueRequest>();

                    while (_issueChannel.Reader.TryRead(out var request))
                    {
                        requests.Add(request);
                    }

                    var groupedRequests = requests.GroupBy(r => GetTrimmedTitle(r.Title));

                    foreach (var group in groupedRequests)
                    {
                        var issueTitle = group.Key;
                        var count = group.Count();
                        var firstRequest = group.First();

                        _ = ProcessIssue(issueTitle, firstRequest.Exception, firstRequest.GithubIssueSection, count, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GitHub issue requests.");
            }
        }
    }

    private async Task ProcessIssue(string issueTitle, Exception exception, GithubIssueLabels githubIssueSection, int increment, CancellationToken stoppingToken)
    {
        var semaphore = _issueSemaphores.GetOrAdd(issueTitle, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(stoppingToken);

        try
        {
            var issue = await GetIssueByTittle(issueTitle);

            // Update count if issue exist
            if (issue != null)
            {
                var issueUpdate = new IssueUpdate
                {
                    Title = issue.Title,
                    Body = GetBodyWithCounterUpdated(issue.Body, increment)
                };

                await _githubClient.Issue.Update(_githubSettings.Username, _githubSettings.RepositoryName, issue.Number, issueUpdate);
                return;
            }

            var bodyBuilder = new StringBuilder();
            bodyBuilder.Append("This is an issue created automatically by GrammarNazi when an exception was captured.\n\n");
            bodyBuilder.AppendLine($"Date (UTC): {DateTime.UtcNow}\n\n");
            bodyBuilder.AppendLine("Exception:\n\n").AppendLine(exception.ToString());
            bodyBuilder.AppendLine($"\n\nException caught counter: {increment}.");

            var newIssue = new NewIssue(issueTitle)
            {
                Body = bodyBuilder.ToString()
            };
            newIssue.Labels.Add(GithubIssueLabels.ProductionBug.GetDescription());
            newIssue.Labels.Add(githubIssueSection.GetDescription());

            await _githubClient.Issue.Create(_githubSettings.Username, _githubSettings.RepositoryName, newIssue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing GitHub issue: {issueTitle}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<Issue> GetIssueByTittle(string title)
    {
        var request = new RepositoryIssueRequest
        {
            State = ItemStateFilter.Open
        };

        var issues = await _githubClient.Issue.GetAllForRepository(_githubSettings.Username, _githubSettings.RepositoryName, request);

        return issues.FirstOrDefault(v => v.Title == title);
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

    private static string GetBodyWithCounterUpdated(string issueBody, int increment)
    {
        var index = issueBody.IndexOf("Exception caught counter: ");

        if (index == -1)
        {
            return issueBody + $"\n\nException caught counter: {increment}.";
        }

        var exceptionCaughtText = issueBody[index..];

        var number = exceptionCaughtText.Replace("Exception caught counter: ", "").Replace(".", "");

        if (!int.TryParse(number, out var parsedNumber))
        {
            parsedNumber = 0;
        }

        return issueBody[..index] + $"Exception caught counter: {parsedNumber + increment}.";
    }

    private record IssueRequest(string Title, Exception Exception, GithubIssueLabels GithubIssueSection);
}