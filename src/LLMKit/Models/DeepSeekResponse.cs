// LLMOrchestrator.Models/DeepSeekResponse.cs

using System.Text.Json.Serialization;

namespace LLMKit.Models
{
    internal class DeepSeekResponse
    {
        [JsonPropertyName("choices")]
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();

        [JsonPropertyName("usage")]
        public ChatCompletionUsage? Usage { get; set; }
    }

    internal class Choice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; } = new();

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    internal class Message
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; }
    }
}