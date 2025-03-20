using LLMKit.Models;
using LLMKit.Builders;

namespace LLMKit.Services
{
    /// <summary>
    /// Service for managing conversations and message handling.
    /// </summary>
    public class ConversationService
    {
        /// <summary>
        /// Creates a new conversation with the specified maximum messages.
        /// </summary>
        public Conversation CreateConversation(int maxMessages = Constants.DefaultMaxMessages)
        {
            return new Conversation(maxMessages);
        }

        /// <summary>
        /// Adds a message to the conversation.
        /// </summary>
        public void AddMessage(Conversation conversation, string role, string content)
        {
            ArgumentNullException.ThrowIfNull(conversation, nameof(conversation));
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            
            conversation.AddMessage(role, content);
        }
    }
} 