using LLMKit.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with OpenAI's LLM service.
    /// Supports GPT models through the chat completions API.
    /// </summary>
    public class OpenAIProvider : BaseLLMProvider
    {
        private static readonly Uri DefaultEndpoint = new("https://api.openai.com/v1/chat/completions");
        private const int FirstReasoningGptGeneration = 5;

        // gpt-<major>[.<minor>]..., e.g. gpt-5, gpt-5-mini, gpt-5.1, gpt-6-luna
        private static readonly Regex GptModelName =
            new(@"^gpt-(?<major>\d+)(?:\.(?<minor>\d+))?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // o-series: o1, o3-mini, o4-mini, ... (not "omni-moderation")
        private static readonly Regex OSeriesModelName =
            new(@"^o\d+(?:-|$)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>How a model handles reasoning_effort and the sampling parameters.</summary>
        private readonly record struct ReasoningSupport(bool IsReasoningModel, string? LowestEffort, bool AcceptsEffort = true)
        {
            public static readonly ReasoningSupport Regular = new(false, null);
            public static readonly ReasoningSupport CanTurnOff = new(true, null);
            // Reasons at a fixed effort: rejects other reasoning_effort values and the sampling parameters
            public static readonly ReasoningSupport FixedEffort = new(true, null, AcceptsEffort: false);
            public static ReasoningSupport AlwaysOn(string lowestEffort) => new(true, lowestEffort);

            public bool SupportsNone => IsReasoningModel && AcceptsEffort && LowestEffort == null;
        }

        // Chat Completions models whose name does not tell their reasoning rules (verified on the API, Nov 2026)
        private static readonly (string Prefix, ReasoningSupport Support)[] KnownModels =
        {
            ("gpt-6.1-sol", ReasoningSupport.AlwaysOn("low")),
            ("gpt-6-astra", ReasoningSupport.AlwaysOn("low")),
            ("chat-latest", ReasoningSupport.FixedEffort), // accepts only reasoning_effort "medium" and temperature 1
        };

        private readonly ReasoningSupport _reasoning;

        /// <summary>Initializes a new instance of the OpenAIProvider. Reasoning models are recognized from the model name.</summary>
        /// <param name="apiKey">The OpenAI API key.</param>
        /// <param name="model">The model identifier to use (e.g., "gpt-6-luna").</param>
        /// <param name="endpoint">The chat completions endpoint, or null for the OpenAI endpoint.</param>
        public OpenAIProvider(string apiKey, string model, Uri? endpoint = null)
            : base(apiKey, model, endpoint ?? DefaultEndpoint)
        {
            _reasoning = DetectReasoningSupport(model);
        }

        /// <summary>
        /// Initializes a new instance of the OpenAIProvider for a model name LLMKit cannot classify,
        /// such as a deployment behind an OpenAI-compatible endpoint or a fine-tuned model.
        /// </summary>
        /// <param name="apiKey">The OpenAI API key.</param>
        /// <param name="model">The model identifier to use.</param>
        /// <param name="endpoint">The chat completions endpoint, or null for the OpenAI endpoint.</param>
        /// <param name="isReasoningModel">
        /// True if the model is a reasoning model (receives reasoning_effort, no sampling parameters);
        /// false to treat it as a regular model.
        /// </param>
        public OpenAIProvider(string apiKey, string model, Uri? endpoint, bool isReasoningModel)
            : this(apiKey, model, endpoint)
        {
            if (!isReasoningModel)
                _reasoning = ReasoningSupport.Regular;
            else if (!_reasoning.IsReasoningModel)
                _reasoning = ReasoningSupport.CanTurnOff; // unknown name: the newest behaviour
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

            // Reasoning models reject sampling parameters unless reasoning is turned off ("none").
            // None on a model without "none" is sent as its lowest level. Nulls are omitted when serializing.
            var effort = request.ReasoningEffort;
            bool supportsSampling = SupportsSampling(effort);

            string? reasoningEffort = !_reasoning.IsReasoningModel || !_reasoning.AcceptsEffort || effort is null ? null
                : effort == ReasoningEffort.None ? _reasoning.LowestEffort ?? ReasoningEffort.None.ToApiValue()
                : effort.Value.ToApiValue();

            return new
            {
                model = Model,
                messages = formattedMessages,
                max_completion_tokens = request.MaxTokens,
                reasoning_effort = reasoningEffort,
                temperature = supportsSampling ? request.Temperature : (double?)null,
                top_p = supportsSampling ? request.TopP : (double?)null,
                frequency_penalty = supportsSampling ? request.FrequencyPenalty : (double?)null,
                presence_penalty = supportsSampling ? request.PresencePenalty : (double?)null,
                stream = request.Stream
            };
        }

        /// <inheritdoc/>
        public override ProviderCapabilities GetCapabilities(LLMRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            return new ProviderCapabilities
            {
                SupportsReasoningEffort = _reasoning.IsReasoningModel && _reasoning.AcceptsEffort,
                CanTurnOffReasoning = _reasoning.SupportsNone,
                SupportsTemperature = SupportsSampling(request.ReasoningEffort),
                ReturnsReasoningText = false, // Chat Completions does not return the reasoning
                SupportsImages = true
            };
        }

        // Regular models, and reasoning models with reasoning turned off, accept the sampling parameters
        private bool SupportsSampling(ReasoningEffort? effort) =>
            !_reasoning.IsReasoningModel || (effort == ReasoningEffort.None && _reasoning.SupportsNone);

        private static ReasoningSupport DetectReasoningSupport(string model)
        {
            var name = model.ToLowerInvariant();

            foreach (var (prefix, support) in KnownModels)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                    return support;
            }

            if (OSeriesModelName.IsMatch(name))
                return ReasoningSupport.AlwaysOn("low");

            var gpt = GptModelName.Match(name);
            if (!gpt.Success || !int.TryParse(gpt.Groups["major"].Value, out int major) || major < FirstReasoningGptGeneration)
                return ReasoningSupport.Regular;

            if (name.StartsWith($"gpt-{major}-chat", StringComparison.Ordinal))
                return ReasoningSupport.Regular;

            // GPT-5.0 (gpt-5, gpt-5-mini, gpt-5-nano) has "minimal" instead of "none"
            bool hasMinor = int.TryParse(gpt.Groups["minor"].Value, out int minor);
            return major == FirstReasoningGptGeneration && (!hasMinor || minor == 0)
                ? ReasoningSupport.AlwaysOn("minimal")
                : ReasoningSupport.CanTurnOff;
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
            var finishReason = FinishReasonParser.FromChatCompletion(responseObject?.Choices?.FirstOrDefault()?.FinishReason);

            if (!IsValidResponse(responseObject))
            {
                return new LLMResponse { Text = string.Empty, Usage = responseObject?.Usage?.ToTokenUsage(), FinishReason = finishReason };
            }

            return new LLMResponse
            {
                Text = responseObject!.Choices[0].Message.Content?.Trim() ?? string.Empty,
                Usage = responseObject.Usage?.ToTokenUsage(),
                FinishReason = finishReason
            };
        }

        private static bool IsValidResponse(OpenAIResponse? response)
        {
            return response?.Choices != null
                && response.Choices.Length > 0
                && response.Choices[0]?.Message?.Content != null
                && !string.IsNullOrWhiteSpace(response.Choices[0].Message.Content);
        }
    }
}