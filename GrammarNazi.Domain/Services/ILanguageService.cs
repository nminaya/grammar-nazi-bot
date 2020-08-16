using GrammarNazi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Services
{
    public interface ILanguageService
    {
        LanguageInformation IdentifyLanguage(string text);
    }
}
