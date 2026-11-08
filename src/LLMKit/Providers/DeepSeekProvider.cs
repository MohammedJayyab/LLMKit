// Providers/DeepSeekProvider.cs
using LLMKit.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with DeepSeek's LLM service.
    /// </summary>
    public class DeepSeekProvider : BaseLLMProvider
    {
        private static readonly Uri DefaultEndpoint = new("https://api.deepseek.com/v1/chat/completions");

        /// <summary>Initializes a new instance of the DeepSeekProvider.</summary>
        /// <param name="apiKey">The DeepSeek API key.</param>
        /// <param name="model">The model identifier to use (e.g., "deepseek-flash").</param>
        /// <param name="endpoint">The chat completions endpoint. Defaults to the DeepSeek endpoint.</param>
        public DeepSeekProvider(string apiKey, string model, Uri? endpoint = null)
            : base(apiKey, model, endpoint ?? DefaultEndpoint)
        {
        }

        /// <inheritdoc/>
        protected override void ConfigureHttpClient()
        {
            base.ConfigureHttpClient();
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(Constants.BearerAuth, ApiKey);

            // Higher reasoning effort can take longer than the default timeout
            HttpClient.Timeout = TimeSpan.FromMinutes(3);
        }

        /// <inheritdoc/>
        protected override object CreateRequestData(LLMRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var formattedMessages = request.Messages.Select(FormatChatMessage).ToList();

            return new
            {
                model = Model,
                messages = formattedMessages,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                top_p = request.TopP,
                frequency_penalty = request.FrequencyPenalty,
                presence_penalty = request.PresencePenalty,
                stream = request.Stream,
                // None turns thinking off; other levels turn it on at that effort
                thinking = request.ReasoningEffort.HasValue
                    ? new { type = request.ReasoningEffort == ReasoningEffort.None ? "disabled" : "enabled" }
                    : null,
                reasoning_effort = request.ReasoningEffort is { } effort && effort != ReasoningEffort.None
                    ? effort.ToApiValue()
                    : null
            };
        }

        /// <inheritdoc/>
        public override ProviderCapabilities GetCapabilities(LLMRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            // DeepSeek thinks unless None turns it off (also when no effort is set) and ignores temperature while thinking
            bool thinking = request.ReasoningEffort != ReasoningEffort.None;

            return new ProviderCapabilities
            {
                SupportsReasoningEffort = true,
                CanTurnOffReasoning = true,
                SupportsTemperature = !thinking,
                ReturnsReasoningText = thinking,
                SupportsImages = false // images are dropped, only the text is sent
            };
        }

        private object FormatChatMessage(ChatMessage message)
        {
            // DeepSeek doesn't support multimodal messages in the same format as OpenAI
            if (message.IsMultimodal)
            {
                // Extract all text from the message
                var textContents = message.ContentItems
                    .Where(i => i.Type == MessageContent.ContentType.Text && !string.IsNullOrEmpty(i.Text))
                    .Select(i => i.Text)
                    .ToList();

                if (textContents.Any())
                {
                    // Join all text items with newlines
                    string combinedText = string.Join("\n\n", textContents);

                    return new
                    {
                        role = message.Role,
                        content = combinedText
                    };
                }
            }

            // Simple text-only message or fallback if no text content in multimodal message
            return new
            {
                role = message.Role,
                content = message.Content
            };
        }

        /// <inheritdoc/>
        public override async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestData = CreateRequestData(request);
                var responseJson = await SendRequestAsync(requestData, cancellationToken);
                return ParseResponse(responseJson);
            }
            catch (HttpRequestException ex)
            {
                throw CreateProviderException($"Network error occurred: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw CreateProviderException("Error processing API response.", ex);
            }
            catch (Exception ex)
            {
                throw CreateProviderException($"An unexpected error occurred: {ex.Message}", ex);
            }
        }

        private static LLMResponse ParseResponse(string responseJson)
        {
            var responseObject = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson);
            var finishReason = FinishReasonParser.FromChatCompletion(responseObject?.Choices?.FirstOrDefault()?.FinishReason);

            if (!IsValidResponse(responseObject))
            {
                return new LLMResponse { Text = string.Empty, Usage = responseObject?.Usage?.ToTokenUsage(), FinishReason = finishReason };
            }

            var message = responseObject!.Choices[0].Message;

            return new LLMResponse
            {
                Text = message.Content?.Trim() ?? string.Empty,
                Reasoning = string.IsNullOrWhiteSpace(message.ReasoningContent) ? null : message.ReasoningContent.Trim(),
                Usage = responseObject.Usage?.ToTokenUsage(),
                FinishReason = finishReason
            };
        }

        private static bool IsValidResponse(DeepSeekResponse? response)
        {
            return response?.Choices != null
                && response.Choices.Length > 0
                && response.Choices[0]?.Message?.Content != null
                && !string.IsNullOrWhiteSpace(response.Choices[0].Message.Content);
        }
    }
}