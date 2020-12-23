using GrammarNazi.Domain.Entities.Settings;
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

        public async Task CreateBugIssue(string title, Exception exception)
        {
            var bodyBuilder = new StringBuilder();

            var issue = await GetIssueByTittle(title);

            // Leave a comment in the Issue if Issue exist
            if (issue != null)
            {
                bodyBuilder.Append("GramarNazi Bot: Same exception captured again.\n\n");
                bodyBuilder.AppendLine($"Date (UTC): {DateTime.UtcNow}\n\n");
                bodyBuilder.AppendLine("StackTrace:\n\n").AppendLine(exception.ToString());

                await _githubClient.Issue.Comment.Create(_githubSettings.Username, _githubSettings.RepositoryName, issue.Number, bodyBuilder.ToString());
                return;
            }

            bodyBuilder.Append("This is an issue created automatically by GrammarNazi when an exception was captured.\n\n");
            bodyBuilder.AppendLine($"Date (UTC): {DateTime.UtcNow}\n\n");
            bodyBuilder.AppendLine("Exception:\n\n").AppendLine(exception.ToString());

            var newIssue = new NewIssue(title)
            {
                Body = bodyBuilder.ToString()
            };
            newIssue.Labels.Add("production-bug");

            await _githubClient.Issue.Create(_githubSettings.Username, _githubSettings.RepositoryName, newIssue);
        }

        private async Task<Issue> GetIssueByTittle(string title)
        {
            var issues = await _githubClient.Issue.GetAllForRepository(_githubSettings.Username, _githubSettings.RepositoryName);

            return issues.FirstOrDefault(v => v.Title == title);
        }
    }
}