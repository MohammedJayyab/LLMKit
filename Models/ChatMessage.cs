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
        
        /// <summary>
        /// Gets or sets the list of content items for this message.
        /// This supports multimodal content (text and images).
        /// </summary>
        public List<MessageContent> ContentItems { get; set; } = new List<MessageContent>();

        /// <summary>
        /// Indicates whether this message contains multimodal content (e.g., images)
        /// </summary>
        public bool IsMultimodal => ContentItems.Any(c => c.Type == MessageContent.ContentType.Image);

        public ChatMessage()
        {
        }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
            ContentItems.Add(MessageContent.CreateText(content));
        }
        
        /// <summary>
        /// Creates a chat message with text and optional images
        /// </summary>
        public ChatMessage(string role, string textContent, IEnumerable<MessageContent>? additionalContent = null)
        {
            Role = role;
            Content = textContent;
            ContentItems.Add(MessageContent.CreateText(textContent));
            
            if (additionalContent != null)
            {
                ContentItems.AddRange(additionalContent);
            }
        }
        
        /// <summary>
        /// Adds an image to this message
        /// </summary>
        public void AddImage(string imageUrl, string? mimeType = null)
        {
            ContentItems.Add(MessageContent.CreateImage(imageUrl, mimeType));
        }

        /// <summary>
        /// Removes all images from this message
        /// </summary>
        public void ClearImages()
        {
            ContentItems.RemoveAll(c => c.Type == MessageContent.ContentType.Image);
        }
    }
}