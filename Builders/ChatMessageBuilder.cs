using LLMKit.Models;

namespace LLMKit.Builders
{
    internal sealed class ChatMessageBuilder
    {
        private readonly List<ChatMessage> _messages = new();
        private ChatMessage? _currentMessage = null;

        private ChatMessageBuilder()
        {
        }

        public static ChatMessageBuilder Create()
        {
            return new ChatMessageBuilder();
        }

        public ChatMessageBuilder AddSystemMessage(string content)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            _currentMessage = new ChatMessage(ChatMessage.Roles.System, content);
            _messages.Add(_currentMessage);
            return this;
        }

        public ChatMessageBuilder AddUserMessage(string content)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            _currentMessage = new ChatMessage(ChatMessage.Roles.User, content);
            _messages.Add(_currentMessage);
            return this;
        }

        public ChatMessageBuilder AddAssistantMessage(string content)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            _currentMessage = new ChatMessage(ChatMessage.Roles.Assistant, content);
            _messages.Add(_currentMessage);
            return this;
        }

        /// <summary>
        /// Adds an image to the current message
        /// </summary>
        /// <param name="imageUrl">URL or base64 data of the image</param>
        /// <param name="mimeType">Optional MIME type of the image</param>
        /// <returns>The builder instance for chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when no message exists to add the image to</exception>
        public ChatMessageBuilder AddImage(string imageUrl, string? mimeType = null)
        {
            ArgumentNullException.ThrowIfNull(imageUrl, nameof(imageUrl));

            if (_currentMessage == null)
            {
                throw new InvalidOperationException("Cannot add an image without a message. Add a message first.");
            }

            _currentMessage.AddImage(imageUrl, mimeType);
            return this;
        }

        public IReadOnlyList<ChatMessage> Build()
        {
            return _messages.AsReadOnly();
        }
    }
}