namespace LLMKit.Models
{
    /// <summary>A message in a conversation: its role, text and optional images.</summary>
    public class ChatMessage
    {
        /// <summary>The message roles.</summary>
        public static class Roles
        {
            /// <summary>Instructions for the model.</summary>
            public const string System = "system";
            /// <summary>A message from the user.</summary>
            public const string User = "user";
            /// <summary>A response from the model.</summary>
            public const string Assistant = "assistant";

            /// <summary>All roles.</summary>
            public static readonly string[] All = new[] { System, User, Assistant };
        }

        /// <summary>The role: <see cref="Roles.System"/>, <see cref="Roles.User"/> or <see cref="Roles.Assistant"/>.</summary>
        public string Role { get; set; } = string.Empty;
        /// <summary>The text of the message.</summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>When the message was created (UTC).</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        /// <summary>A unique id for the message.</summary>
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

        /// <summary>Creates an empty message.</summary>
        public ChatMessage()
        {
        }

        /// <summary>Creates a text message.</summary>
        /// <param name="role">The role, one of <see cref="Roles"/>.</param>
        /// <param name="content">The text of the message.</param>
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