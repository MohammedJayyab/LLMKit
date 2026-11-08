using LLMKit.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with LLM Studio's local LLM service.
    /// Supports local models through the OpenAI-compatible chat completions API.
    /// </summary>
    public class LLMStudioProvider : BaseLLMProvider
    {
        private static readonly Uri DefaultEndpoint = new("http://localhost:1234/v1/chat/completions");

        /// <summary>
        /// Initializes a new instance of the LLMStudioProvider.
        /// </summary>
        /// <param name="apiKey">The API key for authentication. Can be empty or null for local servers without authentication.</param>
        /// <param name="model">The model identifier to use (e.g., "glm-4.6v-flash").</param>
        /// <param name="endpoint">The endpoint for the LLM Studio service. Defaults to http://localhost:1234/v1/chat/completions.</param>
        public LLMStudioProvider(string? apiKey, string model, Uri? endpoint = null)
            : base(string.IsNullOrEmpty(apiKey) ? "local" : apiKey, model, endpoint ?? DefaultEndpoint)
        {
            // Only add authorization if an API key is provided (local servers may not require auth).
            // Set here, not in ConfigureHttpClient: the base constructor calls that before apiKey is available.
            if (!string.IsNullOrEmpty(apiKey))
            {
                HttpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(Constants.BearerAuth, apiKey);
            }
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
                reasoning_effort = request.ReasoningEffort?.ToApiValue()
            };
        }

        /// <inheritdoc/>
        public override ProviderCapabilities GetCapabilities(LLMRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            // Local models are unknown to LLMKit: the values say what is sent; the server and model decide the rest
            return new ProviderCapabilities
            {
                SupportsReasoningEffort = true,
                CanTurnOffReasoning = true,
                SupportsTemperature = true,
                ReturnsReasoningText = true,
                SupportsImages = true
            };
        }

        private object FormatChatMessage(ChatMessage message)
        {
            if (!message.IsMultimodal)
            {
                // Simple text-only message
                return new
                {
                    role = message.Role,
                    content = message.Content
                };
            }

            // For multimodal content, create a content array
            var contentList = new List<object>();

            foreach (var item in message.ContentItems)
            {
                if (item.Type == MessageContent.ContentType.Text && !string.IsNullOrEmpty(item.Text))
                {
                    contentList.Add(new
                    {
                        type = "text",
                        text = item.Text
                    });
                }
                else if (item.Type == MessageContent.ContentType.Image && !string.IsNullOrEmpty(item.ImageUrl))
                {
                    string url = item.ImageUrl;
                    string imageUrl = url;

                    // Process image based on format
                    if (url.StartsWith("data:"))
                    {
                        // Already in data URI format - no changes needed
                    }
                    else if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                             (uri.Scheme == "http" || uri.Scheme == "https"))
                    {
                        // For web URLs - no changes needed
                    }
                    else if (File.Exists(url))
                    {
                        // For local file paths, read and convert to data URI
                        try
                        {
                            string mimeType = item.ImageMimeType ?? GetMimeTypeFromExtension(Path.GetExtension(url));
                            byte[] imageBytes = File.ReadAllBytes(url);
                            string base64Data = Convert.ToBase64String(imageBytes);
                            imageUrl = $"data:{mimeType};base64,{base64Data}";
                        }
                        catch (Exception)
                        {
                            continue; // Skip this image if it can't be loaded
                        }
                    }
                    else
                    {
                        // For raw base64, add required data URI prefix
                        var mimeType = item.ImageMimeType ?? "image/jpeg";
                        imageUrl = $"data:{mimeType};base64,{url}";
                    }

                    contentList.Add(new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = imageUrl
                        }
                    });
                }
            }

            return new
            {
                role = message.Role,
                content = contentList
            };
        }

        private string GetMimeTypeFromExtension(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg" // Default to JPEG
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
                throw CreateProviderException(ex.Message, ex);
            }
            catch (JsonException ex)
            {
                throw CreateProviderException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw CreateProviderException(ex.Message, ex);
            }
        }

        private static LLMResponse ParseResponse(string responseJson)
        {
            var responseObject = JsonSerializer.Deserialize<LLMStudioResponse>(responseJson);
            var finishReason = FinishReasonParser.FromChatCompletion(responseObject?.Choices?.FirstOrDefault()?.FinishReason);

            if (!IsValidResponse(responseObject))
            {
                return new LLMResponse { Text = string.Empty, Usage = responseObject?.Usage?.ToTokenUsage(), FinishReason = finishReason };
            }

            var message = responseObject!.Choices[0].Message;
            var reasoning = message.Reasoning ?? message.ReasoningContent;

            return new LLMResponse
            {
                Text = message.Content?.Trim() ?? string.Empty,
                Reasoning = string.IsNullOrWhiteSpace(reasoning) ? null : reasoning.Trim(),
                Usage = responseObject.Usage?.ToTokenUsage(),
                FinishReason = finishReason
            };
        }

        private static bool IsValidResponse(LLMStudioResponse? response)
        {
            return response?.Choices != null
                && response.Choices.Length > 0
                && response.Choices[0]?.Message?.Content != null
                && !string.IsNullOrWhiteSpace(response.Choices[0].Message.Content);
        }
    }
}