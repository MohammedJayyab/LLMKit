using System.Text.Json.Serialization;

namespace LLMKit.Models
{
    internal class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public Candidate[] Candidates { get; set; } = Array.Empty<Candidate>();

        [JsonPropertyName("usageMetadata")]
        public GeminiUsageMetadata? UsageMetadata { get; set; }

        // Set instead of candidates when the prompt itself is blocked
        [JsonPropertyName("promptFeedback")]
        public GeminiPromptFeedback? PromptFeedback { get; set; }
    }

    internal class GeminiPromptFeedback
    {
        [JsonPropertyName("blockReason")]
        public string? BlockReason { get; set; }
    }

    internal class GeminiUsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        // Excludes the thinking tokens, which are counted in thoughtsTokenCount
        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("thoughtsTokenCount")]
        public int? ThoughtsTokenCount { get; set; }

        internal TokenUsage ToTokenUsage() => new()
        {
            InputTokens = PromptTokenCount,
            OutputTokens = CandidatesTokenCount + (ThoughtsTokenCount ?? 0),
            ReasoningTokens = ThoughtsTokenCount
        };
    }

    internal class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; } = new();

        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }

    internal class Content
    {
        [JsonPropertyName("parts")]
        public Part[] Parts { get; set; } = Array.Empty<Part>();
    }

    internal class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("thought")]
        public bool Thought { get; set; }
    }
}