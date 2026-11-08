using System.Text.Json.Serialization;

namespace LLMKit.Models
{
    /// <summary>
    /// The usage block of OpenAI-compatible chat completion responses (OpenAI, DeepSeek, LLM Studio).
    /// </summary>
    internal class ChatCompletionUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        // Includes the reasoning tokens
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("completion_tokens_details")]
        public TokenDetails? CompletionTokensDetails { get; set; }

        public class TokenDetails
        {
            [JsonPropertyName("reasoning_tokens")]
            public int? ReasoningTokens { get; set; }
        }

        internal TokenUsage ToTokenUsage() => new()
        {
            InputTokens = PromptTokens,
            OutputTokens = CompletionTokens,
            ReasoningTokens = CompletionTokensDetails?.ReasoningTokens
        };
    }
}
