using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.IO;

namespace GrammarNazi.Core.Services
{
    public class FileService : IFileService
    {
        public string GetTextFile(string path)
        {
            return File.ReadAllText(path);
        }

        public IEnumerable<string> GetTextFileByLine(string path)
        {
            var fileStream = new FileStream(path, FileMode.Open);
            using var reader = new StreamReader(fileStream);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}