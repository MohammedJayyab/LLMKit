// LLMOrchestratorClient.cs
using LLMKit.Models;
using LLMKit.Providers;
using LLMKit.Exceptions;
using System.Text;

namespace LLMKit
{
    /// <summary>
    /// Main client for interacting with LLM providers.
    /// </summary>
    public class LLMClient : IDisposable
    {
        private readonly ILLMProvider _provider;
        private readonly object _conversationLock = new();
        private Conversation _currentConversation;
        private readonly int _maxTokens;
        private int _maxMessages;
        private readonly double _temperature;
        private readonly ReasoningEffort? _reasoningEffort;
        // The last response, written as one reference so the Last* properties always describe the same call
        private LLMResponse? _lastResponse;
        private bool _disposed;

        private const string ProviderSuffix = "Provider";

        // Every public provider class in LLMKit, without the "Provider" suffix (OpenAIProvider -> "OpenAI")
        private static readonly IReadOnlyList<string> SupportedProviders = typeof(ILLMProvider).Assembly
            .GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(ILLMProvider).IsAssignableFrom(type))
            .Select(type => type.Name.EndsWith(ProviderSuffix, StringComparison.Ordinal)
                ? type.Name[..^ProviderSuffix.Length]
                : type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();

        /// <summary>
        /// Gets the reasoning returned with the last response, or null if the provider returned none.
        /// Null after a failed call.
        /// </summary>
        public string? LastReasoning => Volatile.Read(ref _lastResponse)?.Reasoning;

        /// <summary>
        /// Gets the token counts of the last response, or null if the provider returned none.
        /// Null after a failed call.
        /// </summary>
        public TokenUsage? LastUsage => Volatile.Read(ref _lastResponse)?.Usage;

        /// <summary>
        /// Gets why the model stopped for the last response, or null if the provider did not report it.
        /// <see cref="FinishReason.MaxTokens"/> means the answer is cut off, or empty when the reasoning used all the tokens.
        /// Null after a failed call.
        /// </summary>
        public FinishReason? LastFinishReason => Volatile.Read(ref _lastResponse)?.FinishReason;

        /// <summary>
        /// Gets what the provider does for its model with this client's settings (reasoning effort, max tokens),
        /// for example whether the reasoning effort and the temperature are applied.
        /// </summary>
        public ProviderCapabilities Capabilities
        {
            get
            {
                ThrowIfDisposed();
                return _provider.GetCapabilities(CreateRequest(Array.Empty<ChatMessage>()));
            }
        }

        /// <summary>
        /// Gets the current conversation.
        /// </summary>
        public Conversation CurrentConversation
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
        /// Initializes a new instance of the LLMClient.
        /// </summary>
        /// <param name="provider">The LLM provider to use.</param>
        /// <param name="maxTokens">
        /// Maximum tokens the model may generate per response, reasoning plus answer (the input is not counted).
        /// Defaults to Constants.DefaultMaxTokens (4096). No upper limit: the provider rejects values above the model's limit.
        /// </param>
        /// <param name="temperature">Temperature. Defaults to Constants.DefaultTemperature.</param>
        /// <param name="maxMessages">Maximum messages kept in the conversation.</param>
        /// <param name="reasoningEffort">Reasoning effort. Null uses the model's default.</param>
        public LLMClient(
            ILLMProvider provider,
            int maxTokens = Constants.DefaultMaxTokens,
            double temperature = Constants.DefaultTemperature,
             int maxMessages = Constants.DefaultMaxMessages,
            ReasoningEffort? reasoningEffort = null
            )
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _maxTokens = maxTokens;
            _temperature = temperature;
            _maxMessages = maxMessages;
            _reasoningEffort = reasoningEffort;
            _currentConversation = new Conversation(maxMessages);

            // Initialize with system message
            _currentConversation.AddMessage(ChatMessage.Roles.System, Constants.SystemMessage);
        }

        /// <summary>
        /// Sets or updates the system message in the conversation.
        /// </summary>
        /// <param name="systemMessage">The system message to set.</param>
        public void SetSystemMessage(string? systemMessage = Constants.SystemMessage)
        {
            ThrowIfDisposed();

            string message = systemMessage ?? Constants.SystemMessage;

            lock (_conversationLock)
            {
                bool hasSystemMessage = _currentConversation.Messages.Any(msg => msg.Role == ChatMessage.Roles.System);

                if (hasSystemMessage)
                {
                    // Update existing system message
                    var systemMsg = _currentConversation.Messages.First(m => m.Role == ChatMessage.Roles.System);
                    systemMsg.Content = message;
                }
                else
                {
                    // No system message exists, add one
                    _currentConversation.AddMessage(ChatMessage.Roles.System, message);
                }
            }
        }

        /// <summary>
        /// Changes the maximum number of messages kept in the conversation and removes the oldest messages if needed.
        /// </summary>
        /// <param name="maxMessages">The new maximum; must be greater than 0.</param>
        public void UpdateMaxMessages(int maxMessages)
        {
            ThrowIfDisposed();
            if (maxMessages < 0)
            {
                throw new ArgumentException("Max messages must be greater than 0", nameof(maxMessages));
            }
            lock (_conversationLock)
            {
                _maxMessages = maxMessages;
                _currentConversation.UpdateMaxMessages(maxMessages);
            }
        }

        /// <summary>
        /// Gets the maximum number of messages kept in the conversation.
        /// </summary>
        public int GetMaxMessages()
        {
            ThrowIfDisposed();
            lock (_conversationLock)
            {
                return _maxMessages;
            }
        }

        /// <summary>
        /// Gets the names of the providers implemented in LLMKit, sorted alphabetically,
        /// e.g. "OpenAI" for <see cref="OpenAIProvider"/>.
        /// </summary>
        /// <returns>The provider names.</returns>
        public static IReadOnlyList<string> GetSupportedProviders() => SupportedProviders;

        /// <summary>
        /// Gets a formatted string with all the client settings.
        /// </summary>
        /// <returns>A formatted string containing the current client configuration.</returns>
        public string GetAllSettings()
        {
            ThrowIfDisposed();

            var sb = new StringBuilder();
            sb.AppendLine("LLMClient Configuration:");
            sb.AppendLine($"Max Tokens:    {_maxTokens}");
            sb.AppendLine($"Temperature:   {_temperature:F2}");
            sb.AppendLine($"Max Messages:  {_maxMessages}");
            sb.AppendLine($"Provider:      {_provider.GetType().Name}");
            // sb.AppendLine($"Endpoint:      {_provider.Endpoint}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates a response to the user's message.
        /// </summary>
        /// <param name="userMessage">The user's message.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The generated response.</returns>
        public async Task<string> GenerateTextAsync(
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(userMessage, nameof(userMessage));

            // Add user message to conversation
            lock (_conversationLock)
            {
                _currentConversation.AddMessage(ChatMessage.Roles.User, userMessage);
            }

            // Get messages from conversation
            IReadOnlyList<ChatMessage> messages;
            lock (_conversationLock)
            {
                messages = _currentConversation.Messages;
            }

            // Generate response (outside lock)
            string response = await GenerateTextInternalAsync(messages, cancellationToken);

            AddAssistantResponse(response);

            return response;
        }

        /// <summary>
        /// Generates a response to a user message with an image.
        /// </summary>
        /// <param name="userMessage">The user's text message.</param>
        /// <param name="imagePath">Path to the image file.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The generated response.</returns>
        public async Task<string> GenerateTextWithImageAsync(
            string userMessage,
            string imagePath,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(userMessage, nameof(userMessage));
            ArgumentNullException.ThrowIfNull(imagePath, nameof(imagePath));

            // Create a user message with text and image
            var message = new ChatMessage(ChatMessage.Roles.User, userMessage);
            message.AddImage(imagePath);

            // Add the message to the conversation
            lock (_conversationLock)
            {
                _currentConversation.AddMessage(message);
            }

            // Get messages from conversation
            IReadOnlyList<ChatMessage> messages;
            lock (_conversationLock)
            {
                messages = _currentConversation.Messages;
            }

            // Generate response (outside lock)
            string response = await GenerateTextInternalAsync(messages, cancellationToken);

            // Clear the image from the message after successful response
            lock (_conversationLock)
            {
                // Find the message we just added and clear its image
                var lastUserMessage = _currentConversation.Messages
                    .LastOrDefault(m => m.Role == ChatMessage.Roles.User);
                if (lastUserMessage != null)
                {
                    lastUserMessage.ClearImages();
                }
            }

            AddAssistantResponse(response);

            return response;
        }

        /// <summary>
        /// Clears the conversation history and resets with the default system message.
        /// </summary>
        public void ClearConversation()
        {
            ThrowIfDisposed();
            lock (_conversationLock)
            {
                _currentConversation.Clear();
                _currentConversation.AddMessage(ChatMessage.Roles.System, Constants.SystemMessage);
            }
        }

        /// <summary>
        /// Gets the formatted conversation history.
        /// </summary>
        /// <returns>A string containing the formatted conversation history.</returns>
        public string GetFormattedConversation()
        {
            ThrowIfDisposed();
            lock (_conversationLock)
            {
                return _currentConversation.GetFormattedConversation();
            }
        }

        // An empty answer (the reasoning used all the tokens, or the answer was blocked) is not stored:
        // Anthropic and Gemini reject empty messages in the history, so the next call would fail
        private void AddAssistantResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return;
            }

            lock (_conversationLock)
            {
                _currentConversation.AddMessage(ChatMessage.Roles.Assistant, response);
            }
        }

        private LLMRequest CreateRequest(IEnumerable<ChatMessage> messages) =>
            new LLMRequest()
                .WithMessages(messages)
                .WithMaxTokens(_maxTokens)
                .WithTemperature(_temperature)
                .WithReasoningEffort(_reasoningEffort);

        /// <summary>
        /// Internal implementation to generate text using messages.
        /// </summary>
        private async Task<string> GenerateTextInternalAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            // Cleared first, so a failed call does not leave the previous call's values behind
            Volatile.Write(ref _lastResponse, null);

            try
            {
                var response = await _provider.GenerateTextAsync(CreateRequest(messages), cancellationToken);
                Volatile.Write(ref _lastResponse, response);

                return response.Text;
            }
            catch (Exception ex)
            {
                throw new LLMException($"Failed to generate text: '{ex.Message}'", ex);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LLMClient));
            }
        }

        /// <summary>
        /// Releases all resources used by the LLMClient.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_provider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }

                _disposed = true;
            }
        }
    }
}