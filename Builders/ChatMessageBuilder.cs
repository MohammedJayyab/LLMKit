using System.Text;
using LLMKit.Models;

namespace LLMKit.Builders
{
    internal sealed class ChatMessageBuilder
    {
        private readonly List<ChatMessage> _messages = new();

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
            _messages.Add(new ChatMessage(ChatMessage.Roles.System, content));
            return this;
        }

        public ChatMessageBuilder AddUserMessage(string content)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            _messages.Add(new ChatMessage(ChatMessage.Roles.User, content));
            return this;
        }

        public ChatMessageBuilder AddAssistantMessage(string content)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            _messages.Add(new ChatMessage(ChatMessage.Roles.Assistant, content));
            return this;
        }

        public IReadOnlyList<ChatMessage> Build()
        {
            return _messages.AsReadOnly();
        }
    }
}