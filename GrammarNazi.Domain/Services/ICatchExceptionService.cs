using GrammarNazi.Domain.Enums;
using System;

namespace GrammarNazi.Domain.Services
{
    public interface ICatchExceptionService
    {
        void HandleException(Exception exception, GithubIssueLabels githubIssueSection);
    }
}