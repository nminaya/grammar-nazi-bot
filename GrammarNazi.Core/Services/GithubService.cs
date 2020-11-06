using GrammarNazi.Domain.Services;
using Octokit;
using System;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Services
{
    public class GithubService : IGithubService
    {
        private readonly IGitHubClient _githubClient;

        public GithubService(IGitHubClient githubClient)
        {
            _githubClient = githubClient;
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

            // TODO: get owner and name from config
            await _githubClient.Issue.Create("nminaya", "grammar-nazi-bot", issue);
        }
    }
}