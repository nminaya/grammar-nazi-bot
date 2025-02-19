using Newtonsoft.Json;
using System.Collections.Generic;

namespace GrammarNazi.Domain.Entities.GeminiAPI;

public class GenerateContentResponse
{
    [JsonProperty("candidates")]
    public List<Candidate> Candidates { get; set; }

    [JsonProperty("usageMetadata")]
    public UsageMetadata UsageMetadata { get; set; }

    [JsonProperty("modelVersion")]
    public string ModelVersion { get; set; }
}

public class Candidate
{
    [JsonProperty("content")]
    public ContentResponse Content { get; set; }

    [JsonProperty("finishReason")]
    public string FinishReason { get; set; }

    [JsonProperty("avgLogprobs")]
    public decimal AvgLogprobs { get; set; }
}

public class CandidatesTokensDetail
{
    [JsonProperty("modality")]
    public string Modality { get; set; }

    [JsonProperty("tokenCount")]
    public int TokenCount { get; set; }
}

public class ContentResponse
{
    [JsonProperty("parts")]
    public List<Part> Parts { get; set; }

    [JsonProperty("role")]
    public string Role { get; set; }
}

public class PromptTokensDetail
{
    [JsonProperty("modality")]
    public string Modality { get; set; }

    [JsonProperty("tokenCount")]
    public int TokenCount { get; set; }
}

public class UsageMetadata
{
    [JsonProperty("promptTokenCount")]
    public int PromptTokenCount { get; set; }

    [JsonProperty("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }

    [JsonProperty("totalTokenCount")]
    public int TotalTokenCount { get; set; }

    [JsonProperty("promptTokensDetails")]
    public List<PromptTokensDetail> PromptTokensDetails { get; set; }

    [JsonProperty("candidatesTokensDetails")]
    public List<CandidatesTokensDetail> CandidatesTokensDetails { get; set; }
}
