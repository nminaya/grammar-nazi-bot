using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.OpenAiAPI;

public class OpenAiChatCompletionResponse
{
    public List<Choice> Choices { get; set; }

    public class Choice
    {
        public Message Message { get; set; }
    }

    public class Message
    {
        public string Content { get; set; }
    }
}
