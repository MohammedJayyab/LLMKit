using LLMKit.Models;
using System.Text.Json;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with Anthropic's Claude models through the Messages API.
    /// </summary>
    public class AnthropicProvider : BaseLLMProvider
    {
        private static readonly Uri DefaultEndpoint = new("https://api.anthropic.com/v1/messages");
        private const string ApiVersion = "2023-06-01";

        /// <summary>
        /// Initializes a new instance of the AnthropicProvider.
        /// </summary>
        /// <param name="apiKey">The Anthropic API key.</param>
        /// <param name="model">The model identifier to use (e.g., "claude-opus-5-5").</param>
        /// <param name="endpoint">The Messages API endpoint. Defaults to the Anthropic endpoint.</param>
        public AnthropicProvider(string apiKey, string model, Uri? endpoint = null)
            : base(apiKey, model, endpoint ?? DefaultEndpoint)
        {
        }

        protected override void ConfigureHttpClient()
        {
            base.ConfigureHttpClient();
            HttpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            HttpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);

            // Higher reasoning effort can take longer than the default timeout
            HttpClient.Timeout = TimeSpan.FromMinutes(3);
        }

        protected override object CreateRequestData(LLMRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            // The system prompt is a top-level field, not a message
            var systemMessage = string.Join("\n\n", request.Messages
                .Where(m => m.Role == ChatMessage.Roles.System)
                .Select(m => m.Content));

            var formattedMessages = request.Messages
                .Where(m => m.Role != ChatMessage.Roles.System)
                .Select(FormatChatMessage)
                .ToList();

            // Newer models reject temperature; nulls are omitted when serializing
            bool supportsSampling = SupportsSamplingParameters(Model);
            var effort = request.ReasoningEffort;

            return new
            {
                model = Model,
                max_tokens = request.MaxTokens,
                system = string.IsNullOrEmpty(systemMessage) ? null : systemMessage,
                messages = formattedMessages,
                // Anthropic accepts temperature from 0 to 1 only
                temperature = supportsSampling ? Math.Min(request.Temperature, 1.0) : (double?)null,
                // Thinking is adaptive; None leaves it unset and uses the lowest effort
                thinking = effort.HasValue && effort != ReasoningEffort.None
                    ? new { type = "adaptive", display = "summarized" }
                    : null,
                output_config = effort.HasValue
                    ? new { effort = effort == ReasoningEffort.None ? "low" : effort.Value.ToApiValue() }
                    : null
            };
        }

        private static bool SupportsSamplingParameters(string model)
        {
            var name = model.ToLowerInvariant();

            // Only older models still accept sampling parameters; claude-opus-4-7 and later reject them
            if (name.StartsWith("claude-opus-4-7", StringComparison.Ordinal)
                || name.StartsWith("claude-opus-4-8", StringComparison.Ordinal))
                return false;

            return name.StartsWith("claude-3", StringComparison.Ordinal)
                || name.StartsWith("claude-opus-4", StringComparison.Ordinal)
                || name.StartsWith("claude-sonnet-4", StringComparison.Ordinal)
                || name.StartsWith("claude-haiku-4", StringComparison.Ordinal);
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
                    var imageBlock = CreateImageBlock(item);
                    if (imageBlock != null)
                    {
                        contentList.Add(imageBlock);
                    }
                }
            }

            return new
            {
                role = message.Role,
                content = contentList
            };
        }

        private object? CreateImageBlock(MessageContent item)
        {
            string url = item.ImageUrl!;

            // Web URL
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                return new { type = "image", source = new { type = "url", url } };
            }

            string mimeType;
            string base64Data;

            if (url.StartsWith("data:"))
            {
                // Data URI: data:<mime>;base64,<data>
                int separator = url.IndexOf("base64,", StringComparison.Ordinal);
                if (separator < 0)
                    return null;

                mimeType = item.ImageMimeType ?? url.Substring(5, url.IndexOf(';') - 5);
                base64Data = url.Substring(separator + 7);
            }
            else if (File.Exists(url))
            {
                // For local file paths, read and convert to base64
                try
                {
                    mimeType = item.ImageMimeType ?? GetMimeTypeFromExtension(Path.GetExtension(url));
                    base64Data = Convert.ToBase64String(File.ReadAllBytes(url));
                }
                catch (Exception)
                {
                    return null; // Skip this image if it can't be loaded
                }
            }
            else
            {
                // Raw base64
                mimeType = item.ImageMimeType ?? "image/jpeg";
                base64Data = url;
            }

            return new { type = "image", source = new { type = "base64", media_type = mimeType, data = base64Data } };
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
                throw CreateProviderException($"Error processing API response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw CreateProviderException($"An unexpected error occurred: {ex.Message}", ex);
            }
        }

        private static LLMResponse ParseResponse(string responseJson)
        {
            var responseObject = JsonSerializer.Deserialize<AnthropicResponse>(responseJson);

            if (responseObject?.content == null)
            {
                return new LLMResponse { Text = string.Empty };
            }

            var text = string.Concat(responseObject.content.Where(b => b.type == "text").Select(b => b.text)).Trim();
            var reasoning = string.Concat(responseObject.content.Where(b => b.type == "thinking").Select(b => b.thinking)).Trim();

            return new LLMResponse
            {
                Text = text,
                Reasoning = reasoning.Length > 0 ? reasoning : null
            };
        }
    }
}
