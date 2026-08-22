using GrammarNazi.Domain.Enums;

namespace GrammarNazi.Domain.Services;

public interface ICatchExceptionService
{
    void HandleException(Exception exception, GithubIssueLabels githubIssueSection);
}
