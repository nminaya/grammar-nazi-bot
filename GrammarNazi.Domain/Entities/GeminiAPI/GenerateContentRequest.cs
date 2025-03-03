using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.GeminiAPI;

public class GenerateContentRequest
{
    [JsonProperty("contents")]
    public List<Content> Contents { get; set; }

    public static GenerateContentRequest CreateRequestObject(string promt)
    {
        return new()
        {
            Contents = 
            [
                new() 
                {
                    Parts = 
                    [
                        new() 
                        {
                            Text = promt
                        }
                    ]
                }
            ]
        };
    }
}

public class Content
{
    [JsonProperty("parts")]
    public List<Part> Parts { get; set; }
}

public class Part
{
    [JsonProperty("text")]
    public string Text { get; set; }
}
