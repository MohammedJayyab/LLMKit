namespace LLMKit.Models
{
    /// <summary>
    /// Represents the response structure from LLM Studio's local LLM service.
    /// </summary>
    public class LLMStudioResponse
    {
        public Choice[] choices { get; set; } = Array.Empty<Choice>();

        public class Choice
        {
            public Message message { get; set; } = new();
        }

        public class Message
        {
            public string content { get; set; } = string.Empty;

            // Reasoning text is exposed as "reasoning" or "reasoning_content" depending on the LLM Studio version
            public string? reasoning { get; set; }

            public string? reasoning_content { get; set; }
        }
    }
}