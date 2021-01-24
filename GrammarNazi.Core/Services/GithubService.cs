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
            if (title.Length > 256)
                title = title[0..255];

            // Do not duplicate the issue if exist
            if (await IssueExist(title))
                return;

            var bodyBuilder = new StringBuilder();
            bodyBuilder.Append("This is an issue created automatically by GrammarNazi when an exception was captured.\n\n");
            bodyBuilder.AppendLine($"Date (UTC): {DateTime.UtcNow}\n\n");
            bodyBuilder.AppendLine("Exception:\n\n").AppendLine(exception.ToString());

            var issue = new NewIssue(title)
            {
                Body = bodyBuilder.ToString()
            };
            issue.Labels.Add("production-bug");

            await _githubClient.Issue.Create(_githubSettings.Username, _githubSettings.RepositoryName, issue);
        }

        private async Task<bool> IssueExist(string title)
        {
            var issues = await _githubClient.Issue.GetAllForRepository(_githubSettings.Username, _githubSettings.RepositoryName);

            return issues.Any(v => v.Title == title);
        }
    }
}