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
        }
    }
}