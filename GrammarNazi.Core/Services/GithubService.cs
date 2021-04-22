using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class GithubService : IGithubService
    {
        private readonly IGitHubClient _githubClient;
        private readonly GithubSettings _githubSettings;

        public GithubService(IGitHubClient githubClient, IOptions<GithubSettings> options)
        {
            _githubClient = githubClient;
            _githubSettings = options.Value;
        }

        public async Task CreateBugIssue(string title, Exception exception, GithubIssueLabels githubIssueSection)
        {
            var issueTitle = GetTrimmedTitle(title);

            var issue = await GetIssueByTittle(issueTitle);

            // Update count if issue exist
            if (issue != null)
            {
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
            bodyBuilder.AppendLine($"Date (UTC): {DateTime.UtcNow}\n\n");
            bodyBuilder.AppendLine("Exception:\n\n").AppendLine(exception.ToString());
            bodyBuilder.AppendLine("\n\nException caught counter: 1.");

            var newIssue = new NewIssue(issueTitle)
            {
                Body = bodyBuilder.ToString()
            };
            newIssue.Labels.Add(GithubIssueLabels.ProductionBug.GetDescription());
            newIssue.Labels.Add(githubIssueSection.GetDescription());

            await _githubClient.Issue.Create(_githubSettings.Username, _githubSettings.RepositoryName, newIssue);
        }

        private async Task<Issue> GetIssueByTittle(string title)
        {
            var issues = await _githubClient.Issue.GetAllForRepository(_githubSettings.Username, _githubSettings.RepositoryName);

            return issues.FirstOrDefault(v => v.Title == title);
        }

        private static string GetTrimmedTitle(string title)
        {
            if (title.Length <= Defaults.GithubIssueMaxTitleLength)
                return title;

            const string dots = "...";

            return title[0..(Defaults.GithubIssueMaxTitleLength - dots.Length)] + dots;
        }

        private static string GetBodyWithCounterUpdated(string issueBody)
        {
            var index = issueBody.IndexOf("Exception caught counter: ");

            if (index == -1)
            {
                return issueBody + $"\n\nException caught counter: 1.";
            }

            var exceptionCaughtText = issueBody[index..];

            var number = exceptionCaughtText.Replace("Exception caught counter: ", "").Replace(".", "");

            var parsedNumber = int.Parse(number);

            return issueBody[..index] + $"Exception caught counter: {++parsedNumber}.";
        }
    }
}