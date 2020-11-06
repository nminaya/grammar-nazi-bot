using System;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface IGithubService
    {
        Task CreateBugIssue(string title, Exception exception);
    }
}