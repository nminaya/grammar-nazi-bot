using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities
{
    public class LanguageInformation
    {
        public string LanguageCode { get; set; }
        public string Iso639 { get; }
        public string EnglishName { get; }
        public string LocalName { get; }
    }
}
