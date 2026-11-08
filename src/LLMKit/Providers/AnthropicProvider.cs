using LLMKit.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with Anthropic's Claude models through the Messages API.
    /// </summary>
    public class AnthropicProvider : BaseLLMProvider
    {
        private static readonly Uri DefaultEndpoint = new("https://api.anthropic.com/v1/messages");
        private const string ApiVersion = "2023-06-01";

        private enum ThinkingMode { NotSupported, Manual, Adaptive }

        // claude-<family>-<major>[-<minor>]..., e.g. claude-sonnet-4-5-20250929, claude-opus-5-5
        // (an 8-digit date is not a minor version: claude-opus-4-20250514 is 4.0)
        private static readonly Regex CurrentModelName = new(
            @"^claude-(?<family>[a-z]+)-(?<major>\d+)(?:-(?<minor>\d{1,2})(?!\d))?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // claude-<major>[-<minor>]-<family>..., e.g. claude-3-7-sonnet-20250219, claude-3-opus
        private static readonly Regex LegacyModelName = new(
            @"^claude-(?<major>\d+)(?:-(?<minor>\d))?-(?<family>[a-z]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Version FirstManualThinkingVersion = new(3, 7);
        private static readonly Version FirstAdaptiveThinkingVersion = new(4, 6);
        private static readonly Version FirstVersionWithoutSampling = new(4, 7);
        private static readonly Version OpusWithManualThinkingAndEffort = new(4, 5);

        private const int MinThinkingBudget = 1024;   // API minimum
        private const int MinAnswerTokens = 1024;     // room left for the answer

        private readonly ThinkingMode _thinking;
        private readonly bool _acceptsSampling;
        private readonly bool _supportsEffort;

        /// <summary>
        /// Initializes a new instance of the AnthropicProvider.
        /// </summary>
        /// <param name="apiKey">The Anthropic API key.</param>
        /// <param name="model">The model identifier to use (e.g., "claude-opus-5-5").</param>
        /// <param name="endpoint">The Messages API endpoint. Defaults to the Anthropic endpoint.</param>
        public AnthropicProvider(string apiKey, string model, Uri? endpoint = null)
            : base(apiKey, model, endpoint ?? DefaultEndpoint)
        {
            (_thinking, _acceptsSampling, _supportsEffort) = DetectModel(model);
        }

        /// <inheritdoc/>
        protected override void ConfigureHttpClient()
        {
            base.ConfigureHttpClient();
            HttpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            HttpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);

            // Higher reasoning effort can take longer than the default timeout
            HttpClient.Timeout = TimeSpan.FromMinutes(3);
        }

        /// <inheritdoc/>
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

            // Nulls are omitted when serializing
            var effort = request.ReasoningEffort;
            var (thinkingOn, budget) = PlanThinking(request);

            object? thinking = !thinkingOn ? null
                : _thinking == ThinkingMode.Adaptive
                    ? new { type = "adaptive", display = "summarized" }
                    : (object)new { type = "enabled", budget_tokens = budget!.Value };

            // Effort only where the model supports it (adaptive models, Opus 4.5); None uses the lowest
            // effort (thinking cannot be disabled on every model, omitting it is always accepted)
            object? outputConfig = _supportsEffort && effort.HasValue
                ? new { effort = effort == ReasoningEffort.None ? "low" : effort.Value.ToApiValue() }
                : null;

            return new
            {
                model = Model,
                max_tokens = request.MaxTokens,
                system = string.IsNullOrEmpty(systemMessage) ? null : systemMessage,
                messages = formattedMessages,
                // Anthropic accepts temperature from 0 to 1 only, and not together with thinking
                temperature = _acceptsSampling && !thinkingOn ? Math.Min(request.Temperature, 1.0) : (double?)null,
                thinking,
                output_config = outputConfig
            };
        }

        /// <inheritdoc/>
        public override ProviderCapabilities GetCapabilities(LLMRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            bool thinkingOn = PlanThinking(request).On;

            // Extended thinking (Claude 3.7 to 4.5) needs room for a budget in max_tokens, otherwise the effort is
            // not sent. Opus 4.5 still gets it as output_config.effort.
            bool effortSent = _thinking == ThinkingMode.Adaptive || _supportsEffort
                || (_thinking == ThinkingMode.Manual && ManualThinkingBudget(ReasoningEffort.Low, request.MaxTokens).HasValue);

            return new ProviderCapabilities
            {
                SupportsReasoningEffort = effortSent,
                CanTurnOffReasoning = effortSent, // None leaves thinking unset
                SupportsTemperature = _acceptsSampling && !thinkingOn,
                ReturnsReasoningText = thinkingOn,
                SupportsImages = true
            };
        }

        // Thinking is requested for every level except None; a manual budget must fit in max_tokens
        private (bool On, int? Budget) PlanThinking(LLMRequest request)
        {
            ReasoningEffort? thinkingEffort =
                request.ReasoningEffort is { } level && level != ReasoningEffort.None ? level : null;

            int? budget = _thinking == ThinkingMode.Manual && thinkingEffort.HasValue
                ? ManualThinkingBudget(thinkingEffort.Value, request.MaxTokens)
                : null;

            return (thinkingEffort.HasValue && (_thinking == ThinkingMode.Adaptive || budget.HasValue), budget);
        }

        private static (ThinkingMode Thinking, bool AcceptsSampling, bool SupportsEffort) DetectModel(string model)
        {
            var name = model.ToLowerInvariant();
            var match = CurrentModelName.Match(name);
            if (!match.Success)
                match = LegacyModelName.Match(name);

            if (!match.Success || !int.TryParse(match.Groups["major"].Value, out int major))
                return (ThinkingMode.Adaptive, false, true); // unknown name: the newest behaviour

            int minor = int.TryParse(match.Groups["minor"].Value, out int m) ? m : 0;
            var version = new Version(major, minor);

            var thinking = version >= FirstAdaptiveThinkingVersion ? ThinkingMode.Adaptive
                         : version >= FirstManualThinkingVersion ? ThinkingMode.Manual
                         : ThinkingMode.NotSupported;

            // Opus 4.5 is the only extended-thinking model that also supports effort
            bool supportsEffort = thinking == ThinkingMode.Adaptive
                || (match.Groups["family"].Value == "opus" && version == OpusWithManualThinkingAndEffort);

            // Claude 4.7 and later reject sampling parameters
            return (thinking, version < FirstVersionWithoutSampling, supportsEffort);
        }

        private static int? ManualThinkingBudget(ReasoningEffort effort, int maxTokens)
        {
            int wanted = effort switch
            {
                ReasoningEffort.Low => 1024,
                ReasoningEffort.Medium => 8192,
                _ => 24576
            };

            // budget_tokens must be at least 1024 and below max_tokens; keep room for the answer
            int budget = Math.Min(wanted, maxTokens - MinAnswerTokens);
            return budget >= MinThinkingBudget ? budget : null;
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

            if (responseObject?.Content == null)
            {
                return new LLMResponse
                {
                    Text = string.Empty,
                    Usage = responseObject?.Usage?.ToTokenUsage(),
                    FinishReason = FinishReasonParser.FromAnthropic(responseObject?.StopReason)
                };
            }

            var text = string.Concat(responseObject.Content.Where(b => b.Type == "text").Select(b => b.Text)).Trim();
            var reasoning = string.Concat(responseObject.Content.Where(b => b.Type == "thinking").Select(b => b.Thinking)).Trim();

            return new LLMResponse
            {
                Text = text,
                Reasoning = reasoning.Length > 0 ? reasoning : null,
                Usage = responseObject.Usage?.ToTokenUsage(),
                FinishReason = FinishReasonParser.FromAnthropic(responseObject.StopReason)
            };
        }
    }
}
