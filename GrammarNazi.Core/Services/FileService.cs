using GrammarNazi.Domain.Services;
using System.Collections.Generic;
using System.IO;

namespace GrammarNazi.Core.Services;

public class FileService : IFileService
{
    public IEnumerable<string> GetTextFileByLine(string path) => File.ReadAllLines(path);

    public bool FileExist(string path) => File.Exists(path);
}