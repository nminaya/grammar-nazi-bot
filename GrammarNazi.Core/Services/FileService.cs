using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GrammarNazi.Core.Services
{
    public class FileService : IFileService
    {
        public string GetTextFile(string path)
        {
            if (!File.Exists(path))
                return string.Empty;

            return File.ReadAllText(path);
        }

        public IEnumerable<string> GetTextFileByLine(string path)
        {
            if (!File.Exists(path))
                return Enumerable.Empty<string>();

            return File.ReadAllLines(path);
        }
    }
}