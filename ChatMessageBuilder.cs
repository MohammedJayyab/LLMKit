using LLMKit.Models;

namespace LLMKit
{
    public class ChatMessageBuilder
    {
        private readonly List<ChatMessage> _messages = new List<ChatMessage>();

        public static ChatMessageBuilder Create()
        {
            return new ChatMessageBuilder();
        }

        public ChatMessageBuilder AddSystemMessage(string content)
        {
            _messages.Add(new ChatMessage(ChatMessage.Roles.System, content));
            return this;
        }

        public ChatMessageBuilder AddUserMessage(string content)
        {
            _messages.Add(new ChatMessage(ChatMessage.Roles.User, content));
            return this;
        }

        public ChatMessageBuilder AddAssistantMessage(string content)
        {
            _messages.Add(new ChatMessage(ChatMessage.Roles.Assistant, content));
            return this;
        }

        public List<ChatMessage> Build()
        {
            return _messages;
        }
    }
}