namespace LLMKit.Models
{
    /// <summary>
    /// Represents the content of a message, which can be text or an image.
    /// </summary>
    public class MessageContent
    {
        /// <summary>
        /// Type of content
        /// </summary>
        public enum ContentType
        {
            Text,
            Image
        }

        /// <summary>
        /// The type of content
        /// </summary>
        public ContentType Type { get; }

        /// <summary>
        /// The text content, if Type is Text
        /// </summary>
        public string? Text { get; }

        /// <summary>
        /// The image URL or base64 data, if Type is Image
        /// </summary>
        public string? ImageUrl { get; }

        /// <summary>
        /// Optional mime type for the image (e.g., "image/jpeg", "image/png")
        /// </summary>
        public string? ImageMimeType { get; }

        /// <summary>
        /// Creates a text content
        /// </summary>
        public static MessageContent CreateText(string text)
        {
            return new MessageContent(ContentType.Text, text, null, null);
        }

        /// <summary>
        /// Creates an image content from URL or Base64 data
        /// </summary>
        /// <param name="imageUrl">URL to an image or Base64 encoded image data</param>
        /// <param name="mimeType">MIME type of the image (optional)</param>
        public static MessageContent CreateImage(string imageUrl, string? mimeType = null)
        {
            return new MessageContent(ContentType.Image, null, imageUrl, mimeType);
        }

        private MessageContent(ContentType type, string? text, string? imageUrl, string? imageMimeType)
        {
            Type = type;
            Text = text;
            ImageUrl = imageUrl;
            ImageMimeType = imageMimeType;
        }
    }
} 