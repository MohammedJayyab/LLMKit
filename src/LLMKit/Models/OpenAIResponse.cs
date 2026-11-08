// LLMOrchestrator.Models/GptResponse.cs
using System.Text.Json.Serialization;

namespace LLMKit.Models
{
    internal class OpenAIResponse
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
        }
    }
}