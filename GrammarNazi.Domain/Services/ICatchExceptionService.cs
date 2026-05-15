using GrammarNazi.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface ICatchExceptionService
    {
        Task HandleException(Exception exception, GithubIssueLabels githubIssueSection);
    }
}