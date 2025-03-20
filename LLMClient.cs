// LLMOrchestratorClient.cs
using LLMKit.Models;
using LLMKit.Providers;

namespace LLMKit
{
    /// <summary>
    /// Main client for interacting with LLM providers.
    /// Provides a simplified interface for text generation using various LLM services.
    /// </summary>
    public class LLMClient
    {
        private readonly ILLMProvider _provider;

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
        /// Initializes a new instance of the LLMOrchestratorClient.
        /// </summary>
        /// <param name="provider">The LLM provider to use for text generation.</param>
        /// <param name="defaultMaxTokens">Optional default maximum tokens.</param>
        /// <param name="defaultTemperature">Optional default temperature.</param>
        public LLMClient(ILLMProvider provider, int? defaultMaxTokens = null, double? defaultTemperature = null)
        {
            _provider = provider;
            DefaultMaxTokens = defaultMaxTokens;
            DefaultTemperature = defaultTemperature;
        }

        /// <summary>
        /// Generates text using the specified system and user messages.
        /// </summary>
        /// <param name="systemMessage">The system message that sets the context.</param>
        /// <param name="userMessage">The user's input message.</param>
        /// <returns>The generated text response.</returns>
        public async Task<string> GenerateTextAsync(string systemMessage, string userMessage)
        {
            List<ChatMessage> messages = ChatMessageBuilder.Create()
                .AddSystemMessage(systemMessage)
                .AddUserMessage(userMessage)
                .Build();

            var request = new LLMRequest()
                .WithMessages(messages)
                .WithMaxTokens(DefaultMaxTokens)
                .WithTemperature(DefaultTemperature);

            var response = await _provider.GenerateTextAsync(request);

            return response.Text;
        }
    }
}