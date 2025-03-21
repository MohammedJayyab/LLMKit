// LLMOrchestratorClient.cs
using LLMKit.Models;
using LLMKit.Providers;
using LLMKit.Services;
using LLMKit.Builders;
using LLMKit.Exceptions;

namespace LLMKit
{
    /// <summary>
    /// Main client for interacting with LLM providers.
    /// Provides a simplified interface for text generation using various LLM services.
    /// </summary>
    /// <remarks>
    /// This class implements IDisposable and should be disposed when no longer needed.
    /// Use the 'using' statement to ensure proper disposal:
    /// <code>
    /// using (var client = new LLMClient(provider))
    /// {
    ///     await client.GenerateTextAsync("Hello");
    /// }
    /// </code>
    /// </remarks>
    public class LLMClient : IDisposable
    {
        private readonly ILLMProvider _provider;
        private readonly ConversationService _conversationService;
        private readonly object _conversationLock = new();
        private Conversation? _currentConversation;
        private readonly int? _defaultMaxTokens;
        private readonly double? _defaultTemperature;
        private bool _disposed;

        /// <summary>
        /// Gets or sets the default maximum number of tokens for text generation.
        /// If not set, uses the provider's default value.
        /// </summary>
        private int? DefaultMaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the default temperature for text generation.
        /// If not set, uses the provider's default value.
        /// </summary>
        private double? DefaultTemperature { get; set; }

        /// <summary>
        /// Gets the current conversation.
        /// </summary>
        public Conversation? CurrentConversation
        {
            get
            {
                lock (_conversationLock)
                {
                    return _currentConversation;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the LLMOrchestratorClient.
        /// </summary>
        /// <param name="provider">The LLM provider to use for text generation.</param>
        /// <param name="defaultMaxTokens">Optional default maximum tokens.</param>
        /// <param name="defaultTemperature">Optional default temperature.</param>
        public LLMClient(ILLMProvider provider, int? defaultMaxTokens = null, double? defaultTemperature = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _defaultMaxTokens = defaultMaxTokens;
            _defaultTemperature = defaultTemperature;
            _conversationService = new ConversationService();
        }

        /// <summary>
        /// Starts a new conversation.
        /// </summary>
        /// <param name="maxMessages">Maximum number of messages to keep in history.</param>
        /// <returns>The new conversation.</returns>
        public Conversation StartConversation(int maxMessages = Constants.DefaultMaxMessages)
        {
            ThrowIfDisposed();
            lock (_conversationLock)
            {
                _currentConversation = _conversationService.CreateConversation(maxMessages);
                return _currentConversation;
            }
        }

        /// <summary>
        /// Generates text using the specified system and user messages.
        /// </summary>
        /// <param name="systemMessage">The system message that sets the context.</param>
        /// <param name="userMessage">The user's input message.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The generated text response.</returns>
        public async Task<string> GenerateTextAsync(string systemMessage, string userMessage, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(systemMessage, nameof(systemMessage));
            ArgumentNullException.ThrowIfNull(userMessage, nameof(userMessage));

            var builder = ChatMessageBuilder.Create();
            builder.AddSystemMessage(systemMessage);
            builder.AddUserMessage(userMessage);
            var messages = builder.Build();

            return await GenerateTextAsync(messages, cancellationToken);
        }

        /// <summary>
        /// Generates text using a list of messages.
        /// </summary>
        /// <param name="messages">The list of messages to use for generation.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The generated text response.</returns>
        private async Task<string> GenerateTextAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            try
            {
                var request = new LLMRequest()
                    .WithMessages(messages)
                    .WithMaxTokens(_defaultMaxTokens)
                    .WithTemperature(_defaultTemperature);

                var response = await _provider.GenerateTextAsync(request, cancellationToken);
                return response.Text;
            }
            catch (Exception ex)
            {
                // Log the error here if you have logging infrastructure
                throw new LLMException("Failed to generate text", ex);
            }
        }

        /// <summary>
        /// Sends a message in the current conversation and gets a response.
        /// </summary>
        /// <param name="message">The user's message.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The generated response.</returns>
        public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(message, nameof(message));

            Conversation? conversation;
            lock (_conversationLock)
            {
                conversation = _currentConversation;
            }

            if (conversation == null)
                throw new InvalidOperationException("No active conversation. Call StartConversation first.");

            // Add user message to conversation
            _conversationService.AddMessage(conversation, ChatMessage.Roles.User, message);

            // Get response using the conversation's messages
            var response = await GenerateTextAsync(conversation.Messages, cancellationToken);

            // Add assistant's response to conversation
            _conversationService.AddMessage(conversation, ChatMessage.Roles.Assistant, response);

            return response;
        }

        /// <summary>
        /// Clears the current conversation.
        /// </summary>
        public void ClearConversation()
        {
            ThrowIfDisposed();
            lock (_conversationLock)
            {
                _currentConversation?.Clear();
            }
        }

        /// <summary>
        /// Gets the formatted conversation history from the current conversation.
        /// </summary>
        /// <returns>A string containing the formatted conversation history, or null if no conversation exists.</returns>
        public string? GetFormattedConversation()
        {
            ThrowIfDisposed();
            lock (_conversationLock)
            {
                return _currentConversation?.GetFormattedConversation();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LLMClient));
        }

        /// <summary>
        /// Releases all resources used by the LLMClient.
        /// </summary>
        /// <remarks>
        /// This method should be called when the client is no longer needed.
        /// It's recommended to use the 'using' statement instead of calling Dispose directly.
        /// </remarks>
        public void Dispose()
        {
            if (!_disposed)
            {
                ClearConversation();
                _disposed = true;
                // Dispose other resources if needed
            }
        }
    }
}