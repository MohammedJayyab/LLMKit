namespace LLMKit.Models
{
    public class ChatMessage
    {
        public static class Roles
        {
            public const string System = "system";
            public const string User = "user";
            public const string Assistant = "assistant";
        }

        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public ChatMessage()
        {
        }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}