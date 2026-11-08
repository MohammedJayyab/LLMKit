using LLMKit.Builders;

namespace LLMKit.Models
{
    /// <summary>
    /// Represents a conversation between a user and an AI assistant.
    /// </summary>
    public class Conversation
    {
        private readonly List<ChatMessage> _messages = new();
        private int _maxMessages;
        private readonly string _id;
        private readonly object _lock = new();

        /// <summary>A unique id for the conversation.</summary>
        public string Id => _id;

        internal IReadOnlyList<ChatMessage> Messages
        {
            get
            {
                lock (_lock)
                {
                    return _messages.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>The number of messages, including the system message.</summary>
        public int MessageCount
        {
            get
            {
                lock (_lock)
                {
                    return _messages.Count;
                }
            }
        }

        /// <summary>Creates an empty conversation.</summary>
        /// <param name="maxMessages">The maximum number of messages kept; older messages are removed, system messages are kept.</param>
        public Conversation(int maxMessages = Constants.DefaultMaxMessages)
        {
            if (maxMessages < 0)
            {
                throw new ArgumentException("Max messages must be greater than 0", nameof(maxMessages));
            }
            _maxMessages = maxMessages;
            _id = Guid.NewGuid().ToString("N");
        }

        internal void AddMessage(ChatMessage message)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));
            lock (_lock)
            {
                _messages.Add(message);
                TrimHistory();
            }
        }

        /// <summary>
        /// Adds a message. When the conversation holds more than the maximum, the oldest messages are removed;
        /// system messages are kept.
        /// </summary>
        /// <param name="role">The role, one of <see cref="ChatMessage.Roles"/>.</param>
        /// <param name="content">The text of the message.</param>
        public void AddMessage(string role, string content)
        {
            ArgumentNullException.ThrowIfNull(role, nameof(role));
            ArgumentNullException.ThrowIfNull(content, nameof(content));

            if (!ChatMessage.Roles.All.Contains(role))
            {
                throw new ArgumentException($"Invalid role: {role}. Must be one of: {string.Join(", ", ChatMessage.Roles.All)}", nameof(role));
            }

            var builder = ChatMessageBuilder.Create();
            var message = role switch
            {
                ChatMessage.Roles.System => builder.AddSystemMessage(content).Build().First(),
                ChatMessage.Roles.User => builder.AddUserMessage(content).Build().First(),
                ChatMessage.Roles.Assistant => builder.AddAssistantMessage(content).Build().First(),
                _ => throw new ArgumentException($"Invalid role: {role}", nameof(role))
            };

            AddMessage(message);
        }

        /// <summary>Removes all messages.</summary>
        public void Clear()
        {
            lock (_lock)
            {
                _messages.Clear();
            }
        }

        /// <summary>
        /// Gets the formatted conversation history.
        /// </summary>
        /// <returns>A string containing the formatted conversation history.</returns>
        public string GetFormattedConversation()
        {
            lock (_lock)
            {
                if (_messages.Count == 0)
                    return string.Empty;

                return string.Join(Environment.NewLine + Environment.NewLine,
                    _messages.Select(message => $"{message.Role}: {message.Content}"));
            }
        }

        /// <summary>Changes the maximum number of messages kept and removes the oldest messages if needed.</summary>
        /// <param name="newMaxMessages">The new maximum; must be greater than 0.</param>
        public void UpdateMaxMessages(int newMaxMessages)
        {
            if (newMaxMessages <= 0)
            {
                throw new ArgumentException("Max messages must be greater than 0", nameof(newMaxMessages));
            }

            lock (_lock)
            {
                _maxMessages = newMaxMessages;
                TrimHistory();
            }
        }

        private void TrimHistory()
        {
            if (_messages.Count <= _maxMessages) return;

            var systemMessages = _messages.Where(m => m.Role == ChatMessage.Roles.System).ToList();

            var recentMessages = _messages
                .Where(m => m.Role != ChatMessage.Roles.System)
                .OrderByDescending(m => m.Timestamp)
                .Take(_maxMessages - systemMessages.Count)
                .OrderBy(m => m.Timestamp)
                .ToList();

            _messages.Clear();
            _messages.AddRange(systemMessages);
            _messages.AddRange(recentMessages);
        }
    }
}