using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Services;

public interface IGithubService
{
    Task CreateBugIssue(string title, Exception exception, GithubIssueLabels githubIssueSection);
}
