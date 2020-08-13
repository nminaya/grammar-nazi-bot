using System.Collections.Generic;

namespace GrammarNazi.Domain.Services
{
    public interface IFileService
    {
        IEnumerable<string> GetTextFileByLine(string path);

        string GetTextFile(string path);
    }
}