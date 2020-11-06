using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Options;
using Octokit;
using System;
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
            bodyBuilder.Append("This is an automated issue created by GrammarNazi when an exception was captured.");
            bodyBuilder.AppendLine($"Date (UTC): {DateTime.UtcNow}");
            bodyBuilder.AppendLine("Exception:").AppendLine(exception.StackTrace);

            var issue = new NewIssue(title)
            {
                Body = bodyBuilder.ToString()
            };
            issue.Labels.Add("bug");

            await _githubClient.Issue.Create(_githubSettings.Username, _githubSettings.RepositoryName, issue);
        }
    }
}