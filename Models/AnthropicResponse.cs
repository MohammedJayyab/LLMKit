namespace LLMKit.Models
{
    /// <summary>
    /// Represents the response structure from Anthropic's Messages API.
    /// </summary>
    public class AnthropicResponse
    {
        public ContentBlock[] content { get; set; } = Array.Empty<ContentBlock>();

        public class ContentBlock
        {
            public string type { get; set; } = string.Empty;

            public string? text { get; set; }

            public string? thinking { get; set; }
        }
    }
}
