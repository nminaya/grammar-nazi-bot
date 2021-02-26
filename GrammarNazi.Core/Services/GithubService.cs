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

            // Do not duplicate the issue if exist
            if (await IssueExist(issueTitle))
                return;

            var bodyBuilder = new StringBuilder();
            bodyBuilder.Append("This is an issue created automatically by GrammarNazi when an exception was captured.\n\n");
            bodyBuilder.AppendLine($"Date (UTC): {DateTime.UtcNow}\n\n");
            bodyBuilder.AppendLine("Exception:\n\n").AppendLine(exception.ToString());

            var issue = new NewIssue(issueTitle)
            {
                Body = bodyBuilder.ToString()
            };
            issue.Labels.Add(GithubIssueLabels.ProductionBug.GetDescription());
            issue.Labels.Add(githubIssueSection.GetDescription());

            await _githubClient.Issue.Create(_githubSettings.Username, _githubSettings.RepositoryName, issue);
        }

        private async Task<bool> IssueExist(string title)
        {
            var issues = await _githubClient.Issue.GetAllForRepository(_githubSettings.Username, _githubSettings.RepositoryName);

            return issues.Any(v => v.Title == title);
        }

        private static string GetTrimmedTitle(string title)
        {
            if (title.Length <= Defaults.GithubIssueMaxTitleLength)
                return title;

            const string dots = "...";

            return title[0..(Defaults.GithubIssueMaxTitleLength - dots.Length)] + dots;
        }
    }
}