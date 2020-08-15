using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarNazi.Domain.Entities
{
    public class ChatConfiguration
    {
        public long ChatId { get; set; }

        public GrammarAlgorithms GrammarAlgorithm { get; set; }
    }
}
