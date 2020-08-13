using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface IGrammarService
    {
        Task<IEnumerable<string>> GetCorrections(string text);
    }
}
