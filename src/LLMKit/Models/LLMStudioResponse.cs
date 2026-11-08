using System.Text.Json.Serialization;

namespace LLMKit.Models
{
    /// <summary>
    /// Represents the response structure from LLM Studio's local LLM service.
    /// </summary>
    internal class LLMStudioResponse
    {
        [JsonPropertyName("choices")]
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();

        [JsonPropertyName("usage")]
        public ChatCompletionUsage? Usage { get; set; }

        public class Choice
        {
            [JsonPropertyName("message")]
            public Message Message { get; set; } = new();

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        public class Message
        {
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;

            // Reasoning text is exposed as "reasoning" or "reasoning_content" depending on the LLM Studio version
            [JsonPropertyName("reasoning")]
            public string? Reasoning { get; set; }

            [JsonPropertyName("reasoning_content")]
            public string? ReasoningContent { get; set; }
        }
    }
}