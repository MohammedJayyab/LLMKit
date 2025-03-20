namespace LLMKit.Models
{
    public class ChatMessage
    {
        public static class Roles
        {
            public const string System = "system";
            public const string User = "user";
            public const string Assistant = "assistant";

            public static readonly string[] All = new[] { System, User, Assistant };
        }

        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Id { get; set; } = Guid.NewGuid().ToString();

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