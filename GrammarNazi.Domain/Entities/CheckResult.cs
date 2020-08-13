using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities
{
    public class CheckResult
    {
        public bool HasErrors { get; set; }

        public IEnumerable<ResultError> ResultErrors { get; set; }
    }
}
