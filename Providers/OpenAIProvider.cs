using LLMKit.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with OpenAI's LLM service.
    /// Supports GPT models through the chat completions API.
    /// </summary>
    public class OpenAIProvider : BaseLLMProvider
    {
        private static readonly Uri DefaultEndpoint = new("https://api.openai.com/v1/chat/completions");
        private static readonly string[] ReasoningModelPrefixes = { "gpt-5", "o1", "o3", "o4" };

        public OpenAIProvider(string apiKey, string model, Uri? endpoint = null)
            : base(apiKey, model, endpoint ?? DefaultEndpoint)
        {
        }

        protected override void ConfigureHttpClient()
        {
            base.ConfigureHttpClient();
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(Constants.BearerAuth, ApiKey);
        }

        protected override object CreateRequestData(LLMRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var formattedMessages = request.Messages.Select(FormatChatMessage).ToList();

            // Reasoning models reject sampling parameters unless reasoning is off; nulls are omitted when serializing
            bool isReasoningModel = IsReasoningModel(Model);
            bool supportsSampling = !isReasoningModel || request.ReasoningEffort == ReasoningEffort.None;

            return new
            {
                model = Model,
                messages = formattedMessages,
                max_completion_tokens = request.MaxTokens,
                reasoning_effort = isReasoningModel ? request.ReasoningEffort?.ToApiValue() : null,
                temperature = supportsSampling ? request.Temperature : (double?)null,
                top_p = supportsSampling ? request.TopP : (double?)null,
                frequency_penalty = supportsSampling ? request.FrequencyPenalty : (double?)null,
                presence_penalty = supportsSampling ? request.PresencePenalty : (double?)null,
                stream = request.Stream
            };
        }

        private static bool IsReasoningModel(string model)
        {
            var name = model.ToLowerInvariant();

            if (name.StartsWith("gpt-5-chat", StringComparison.Ordinal))
                return false;

            return ReasoningModelPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal));
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
            var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseJson);

            if (!IsValidResponse(responseObject))
            {
                return new LLMResponse { Text = string.Empty };
            }

            return new LLMResponse
            {
                Text = responseObject!.choices[0].message.content?.Trim() ?? string.Empty
            };
        }

        private static bool IsValidResponse(OpenAIResponse? response)
        {
            return response?.choices != null
                && response.choices.Length > 0
                && response.choices[0]?.message?.content != null
                && !string.IsNullOrWhiteSpace(response.choices[0].message.content);
        }
    }
}