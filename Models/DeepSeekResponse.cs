// LLMOrchestrator.Models/DeepSeekResponse.cs

namespace LLMKit.Models
{
    public class DeepSeekResponse
    {
        public Choice[] choices { get; set; } = Array.Empty<Choice>();
    }

    public class Choice
    {
        public Message message { get; set; } = new();
    }

    public class Message
    {
        public string content { get; set; } = string.Empty;
    }
}