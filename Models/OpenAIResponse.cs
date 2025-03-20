// LLMOrchestrator.Models/GptResponse.cs
namespace LLMKit.Models
{
    public class OpenAIResponse
    {
        public Choice[] choices { get; set; }

        public class Choice
        {
            public Message message { get; set; }
        }

        public class Message
        {
            public string content { get; set; }
        }
    }
}