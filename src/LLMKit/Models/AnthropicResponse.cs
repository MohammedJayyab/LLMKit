using System.Text.Json.Serialization;

namespace LLMKit.Models
{
    /// <summary>
    /// Represents the response structure from Anthropic's Messages API.
    /// </summary>
    internal class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public ContentBlock[] Content { get; set; } = Array.Empty<ContentBlock>();

        public class ContentBlock
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string? Text { get; set; }

            [JsonPropertyName("thinking")]
            public string? Thinking { get; set; }
        }

        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }

        [JsonPropertyName("usage")]
        public UsageInfo? Usage { get; set; }

        public class UsageInfo
        {
            // Excludes the cached tokens, which are counted separately
            [JsonPropertyName("input_tokens")]
            public int InputTokens { get; set; }

            // Includes the thinking tokens; Anthropic does not report them separately
            [JsonPropertyName("output_tokens")]
            public int OutputTokens { get; set; }

            [JsonPropertyName("cache_creation_input_tokens")]
            public int? CacheCreationInputTokens { get; set; }

            [JsonPropertyName("cache_read_input_tokens")]
            public int? CacheReadInputTokens { get; set; }

            internal TokenUsage ToTokenUsage() => new()
            {
                InputTokens = InputTokens + (CacheCreationInputTokens ?? 0) + (CacheReadInputTokens ?? 0),
                OutputTokens = OutputTokens
            };
        }
    }
}
