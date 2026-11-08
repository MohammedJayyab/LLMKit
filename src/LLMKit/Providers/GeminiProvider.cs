// Providers/GeminiProvider.cs
using LLMKit.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with Google's Gemini LLM service.
    /// </summary>
    public class GeminiProvider : BaseLLMProvider
    {
        private static readonly Uri BaseEndpoint = new("https://generativelanguage.googleapis.com/v1beta/models");
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private enum ThinkingControl { NotSupported, Budget, Level }

        // gemini-<major>[.<minor>]..., e.g. gemini-2.5-flash, gemini-3.1-pro-preview, gemini-4-argon
        private static readonly Regex GeminiVersion =
            new(@"^gemini-(?<major>\d+)(?:\.(?<minor>\d+))?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Version FirstBudgetVersion = new(2, 5);
        private static readonly Version FirstLevelVersion = new(3, 0);

        // Levels from lowest to highest
        private static readonly string[] LevelOrder = { "minimal", "low", "medium", "high" };
        private static readonly string[] AllLevels = LevelOrder;
        private static readonly string[] DefaultLevels = { "low", "medium", "high" };
        private static readonly string[] MinimalOrHigh = { "minimal", "high" };

        // Documented thinking levels per model (Gemini / Vertex thinking docs, Nov 2026), longest prefix first
        private static readonly (string Prefix, string[] Levels)[] KnownLevels =
        {
            ("gemini-3.1-flash-lite-image", MinimalOrHigh),
            ("gemini-3.1-flash-image", MinimalOrHigh),
            ("gemini-3.1-flash-lite", AllLevels),
            ("gemini-3.1-pro", DefaultLevels),
            ("gemini-3.5-flash", AllLevels),   // also gemini-3.5-flash-lite
            ("gemini-3.6-flash", AllLevels),
            ("gemini-3-pro-image", new[] { "high" }),
            ("gemini-3-flash", AllLevels),
            ("gemini-3-pro", new[] { "low", "high" }),
            ("gemini-nano-banana", new[] { "minimal", "medium", "high" }),
            ("gemini-robotics-er-2", AllLevels),
        };

        private const int DisabledThinkingBudget = 0;
        private const int MinThinkingBudgetPro25 = 128; // gemini-2.5-pro cannot turn thinking off

        private readonly ThinkingControl _thinking;
        private readonly string[] _levels;
        private readonly int _minBudget;

        /// <summary>Initializes a new instance of the GeminiProvider.</summary>
        /// <param name="apiKey">The Gemini API key.</param>
        /// <param name="model">The model identifier to use (e.g., "gemini-3.6-flash").</param>
        /// <param name="endpoint">A custom endpoint, including the model path. Defaults to the Gemini endpoint for the model.</param>
        public GeminiProvider(string apiKey, string model, Uri? endpoint = null)
            : base(apiKey, model, ConstructEndpoint(endpoint, model, apiKey))
        {
            (_thinking, _levels, _minBudget) = DetectThinking(model);
        }

        private static (ThinkingControl Control, string[] Levels, int MinBudget) DetectThinking(string model)
        {
            var name = model.ToLowerInvariant();

            // Gemma 4 thinks with on/off only ("high" / "minimal"); older Gemma models do not think
            if (name.StartsWith("gemma-4", StringComparison.Ordinal))
                return (ThinkingControl.Level, MinimalOrHigh, DisabledThinkingBudget);
            // Text-to-speech models do not think (Gemini models list, Nov 2026)
            if (!name.StartsWith("gemini-", StringComparison.Ordinal) || name.Contains("-tts", StringComparison.Ordinal))
                return (ThinkingControl.NotSupported, DefaultLevels, DisabledThinkingBudget);

            var match = GeminiVersion.Match(name);
            if (match.Success && int.TryParse(match.Groups["major"].Value, out int major))
            {
                int minor = int.TryParse(match.Groups["minor"].Value, out int m) ? m : 0;
                var version = new Version(major, minor);

                // The 2.5 image models do not think; the 3.x image models do (KnownLevels)
                if (version < FirstBudgetVersion
                    || (version < FirstLevelVersion && name.Contains("-image", StringComparison.Ordinal)))
                    return (ThinkingControl.NotSupported, DefaultLevels, DisabledThinkingBudget);
                if (version < FirstLevelVersion)
                    return (ThinkingControl.Budget, DefaultLevels,
                        name.StartsWith("gemini-2.5-pro", StringComparison.Ordinal) ? MinThinkingBudgetPro25 : DisabledThinkingBudget);
            }

            foreach (var (prefix, levels) in KnownLevels)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                    return (ThinkingControl.Level, levels, DisabledThinkingBudget);
            }

            // Newer or unknown models (and aliases such as gemini-flash-latest): the newest behaviour
            return (ThinkingControl.Level, DefaultLevels, DisabledThinkingBudget);
        }

        // None: the lowest level the model allows. A level the model does not offer: the next higher one, else the highest.
        private string MapLevel(ReasoningEffort effort)
        {
            if (effort == ReasoningEffort.None)
                return _levels[0];

            string wanted = effort.ToApiValue();
            if (_levels.Contains(wanted))
                return wanted;

            int rank = Array.IndexOf(LevelOrder, wanted);
            return _levels.FirstOrDefault(l => Array.IndexOf(LevelOrder, l) > rank) ?? _levels[^1];
        }

        private static Uri ConstructEndpoint(Uri? customEndpoint, string model, string apiKey)
        {
            if (customEndpoint != null)
            {
                var uriBuilder = new UriBuilder(customEndpoint);
                var customQuery = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                customQuery["key"] = apiKey;
                uriBuilder.Query = customQuery.ToString();
                return uriBuilder.Uri;
            }

            var builder = new UriBuilder(BaseEndpoint);
            builder.Path = $"{builder.Path}/{model}:generateContent";
            var defaultQuery = System.Web.HttpUtility.ParseQueryString(builder.Query);
            defaultQuery["key"] = apiKey;
            builder.Query = defaultQuery.ToString();
            
            return builder.Uri;
        }

        /// <inheritdoc/>
        protected override void ConfigureHttpClient()
        {
            // No need to add API key here as it's already in the URI
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            // Increase timeout for larger images
            HttpClient.Timeout = TimeSpan.FromMinutes(2);
            
            // Enable TLS 1.2 and 1.3 which might be required by Google APIs
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        }

        /// <inheritdoc/>
        protected override object CreateRequestData(LLMRequest request)
        {
            // For text-only requests without multimodal content, use simpler format
            if (!request.Messages.Any(m => m.IsMultimodal))
            {
                return CreateTextOnlyRequestData(request);
            }
            
            // Handle multimodal content
            return CreateMultimodalRequestData(request);
        }
        
        private object CreateTextOnlyRequestData(LLMRequest request)
        {
            var contents = new List<Dictionary<string, object>>();
            string? systemMessage = null;
            
            foreach (var message in request.Messages)
            {
                if (message.Role == ChatMessage.Roles.System)
                {
                    systemMessage = message.Content;
                    continue;
                }
                
                contents.Add(new Dictionary<string, object>
                {
                    { "role", message.Role == ChatMessage.Roles.User ? "user" : "model" },
                    { "parts", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "text", message.Content } }
                        }
                    }
                });
            }
            
            var requestObject = new Dictionary<string, object>
            {
                { "contents", contents },
                { "generation_config", CreateGenerationConfig(request) }
            };
            
            if (!string.IsNullOrEmpty(systemMessage))
            {
                requestObject["system_instruction"] = new Dictionary<string, object>
                {
                    { "parts", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "text", systemMessage } }
                        }
                    }
                };
            }
            
            return requestObject;
        }
        
        private object CreateMultimodalRequestData(LLMRequest request)
        {
            var contents = new List<Dictionary<string, object>>();
            string? systemMessage = null;
            
            foreach (var message in request.Messages)
            {
                if (message.Role == ChatMessage.Roles.System)
                {
                    systemMessage = message.Content;
                    continue;
                }
                
                var parts = new List<Dictionary<string, object>>();
                
                if (message.IsMultimodal)
                {
                    foreach (var item in message.ContentItems)
                    {
                        if (item.Type == MessageContent.ContentType.Text && !string.IsNullOrEmpty(item.Text))
                        {
                            parts.Add(new Dictionary<string, object> { { "text", item.Text } });
                        }
                        else if (item.Type == MessageContent.ContentType.Image && !string.IsNullOrEmpty(item.ImageUrl))
                        {
                            AddImagePart(parts, item);
                        }
                    }
                }
                else
                {
                    parts.Add(new Dictionary<string, object> { { "text", message.Content } });
                }
                
                contents.Add(new Dictionary<string, object>
                {
                    { "role", message.Role == ChatMessage.Roles.User ? "user" : "model" },
                    { "parts", parts }
                });
            }
            
            var requestObject = new Dictionary<string, object>
            {
                { "contents", contents },
                { "generation_config", CreateGenerationConfig(request) }
            };
            
            if (!string.IsNullOrEmpty(systemMessage))
            {
                requestObject["system_instruction"] = new Dictionary<string, object>
                {
                    { "parts", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "text", systemMessage } }
                        }
                    }
                };
            }
            
            return requestObject;
        }
        
        private Dictionary<string, object> CreateGenerationConfig(LLMRequest request)
        {
            var config = new Dictionary<string, object>
            {
                { "temperature", request.Temperature },
                { "top_p", request.TopP },
                { "max_output_tokens", request.MaxTokens }
            };

            if (request.ReasoningEffort is { } effort && _thinking != ThinkingControl.NotSupported)
            {
                var thinkingConfig = new Dictionary<string, object>();

                // Gemini 2.5 takes a token budget, newer models take a level (sending both is an error)
                if (_thinking == ThinkingControl.Budget)
                {
                    thinkingConfig["thinking_budget"] = effort switch
                    {
                        ReasoningEffort.None => _minBudget,
                        ReasoningEffort.Low => 1024,
                        ReasoningEffort.Medium => 8192,
                        _ => 24576
                    };
                }
                else
                {
                    thinkingConfig["thinking_level"] = MapLevel(effort);
                }

                if (IncludesThoughts(effort))
                {
                    thinkingConfig["include_thoughts"] = true;
                }

                config["thinking_config"] = thinkingConfig;
            }

            return config;
        }

        /// <inheritdoc/>
        public override ProviderCapabilities GetCapabilities(LLMRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            return new ProviderCapabilities
            {
                SupportsReasoningEffort = _thinking != ThinkingControl.NotSupported,
                // Budget 0 turns thinking off (not on 2.5 Pro). Gemma 4's "minimal" is off; Gemini 3's "minimal" still thinks.
                CanTurnOffReasoning = _thinking == ThinkingControl.Budget
                    ? _minBudget == DisabledThinkingBudget
                    : _thinking == ThinkingControl.Level && IsGemma4,
                SupportsTemperature = true,
                // Gemma 4 thinks and returns its thoughts by default; Gemini models only when they are requested
                ReturnsReasoningText = IncludesThoughts(request.ReasoningEffort) || (request.ReasoningEffort is null && IsGemma4),
                SupportsImages = true
            };
        }

        private bool IsGemma4 => Model.StartsWith("gemma-4", StringComparison.OrdinalIgnoreCase);

        // Thought summaries are requested for every level except None, on models that think
        private bool IncludesThoughts(ReasoningEffort? effort) =>
            _thinking != ThinkingControl.NotSupported && effort is { } level && level != ReasoningEffort.None;

        private void AddImagePart(List<Dictionary<string, object>> parts, MessageContent item)
        {
            string url = item.ImageUrl!;
            
            // Data URI format
            if (url.StartsWith("data:"))
            {
                parts.Add(new Dictionary<string, object>
                {
                    { "inline_data", new Dictionary<string, object>
                        {
                            { "mime_type", item.ImageMimeType ?? GetMimeTypeFromDataUri(url) },
                            { "data", ExtractBase64FromDataUri(url) }
                        }
                    }
                });
                return;
            }
            
            // Web URL
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
                (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                parts.Add(new Dictionary<string, object>
                {
                    { "file_data", new Dictionary<string, object>
                        {
                            { "mime_type", item.ImageMimeType ?? "image/jpeg" },
                            { "file_uri", url }
                        }
                    }
                });
                return;
            }
            
            // Local file path
            if (File.Exists(url))
            {
                try
                {
                    var mimeType = item.ImageMimeType ?? GetMimeTypeFromExtension(Path.GetExtension(url));
                    var imageBytes = File.ReadAllBytes(url);
                    var imageBase64 = Convert.ToBase64String(imageBytes);
                    
                    parts.Add(new Dictionary<string, object>
                    {
                        { "inline_data", new Dictionary<string, object>
                            {
                                { "mime_type", mimeType },
                                { "data", imageBase64 }
                            }
                        }
                    });
                    return;
                }
                catch
                {
                    return; // Skip this image if it can't be loaded
                }
            }
            
            // Raw base64 data
            string rawBase64 = url;
            if (rawBase64.Contains("base64,"))
            {
                rawBase64 = ExtractBase64FromDataUri(rawBase64);
            }
            
            parts.Add(new Dictionary<string, object>
            {
                { "inline_data", new Dictionary<string, object>
                    {
                        { "mime_type", item.ImageMimeType ?? "image/jpeg" },
                        { "data", rawBase64 }
                    }
                }
            });
        }
        
        private string GetMimeTypeFromDataUri(string dataUri)
        {
            int startIndex = dataUri.IndexOf(':') + 1;
            int endIndex = dataUri.IndexOf(';');
            if (startIndex > 0 && endIndex > startIndex)
            {
                return dataUri.Substring(startIndex, endIndex - startIndex);
            }
            return "image/jpeg"; // Default
        }
        
        private string ExtractBase64FromDataUri(string dataUri)
        {
            int base64Index = dataUri.IndexOf("base64,");
            if (base64Index >= 0)
            {
                return dataUri.Substring(base64Index + 7);
            }
            return dataUri; // Return original if not found
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
                var json = JsonSerializer.Serialize(requestData, JsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await HttpClient.PostAsync(Endpoint, content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Error response from Gemini API: {response.StatusCode} - {errorContent}");
                }
                
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
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
            try
        {
            var responseObject = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

            if (!IsValidResponse(responseObject))
            {
                return new LLMResponse
                {
                    Text = string.Empty,
                    Usage = responseObject?.UsageMetadata?.ToTokenUsage(),
                    FinishReason = GetFinishReason(responseObject)
                };
            }

            var parts = responseObject!.Candidates[0].Content.Parts;
            var reasoning = string.Concat(parts.Where(p => p.Thought).Select(p => p.Text)).Trim();

            return new LLMResponse
            {
                Text = string.Concat(parts.Where(p => !p.Thought).Select(p => p.Text)).Trim(),
                Reasoning = reasoning.Length > 0 ? reasoning : null,
                Usage = responseObject.UsageMetadata?.ToTokenUsage(),
                FinishReason = GetFinishReason(responseObject)
            };
            }
            catch (Exception ex)
            {
                throw new JsonException($"Error parsing Gemini response: {ex.Message}", ex);
            }
        }

        // A blocked prompt has no candidates, only promptFeedback.blockReason
        private static FinishReason? GetFinishReason(GeminiResponse? response) =>
            response?.Candidates?.FirstOrDefault()?.FinishReason is { } reason
                ? FinishReasonParser.FromGemini(reason)
                : response?.PromptFeedback?.BlockReason != null ? FinishReason.ContentFilter : null;

        private static bool IsValidResponse(GeminiResponse? response)
        {
            return response?.Candidates != null
                && response.Candidates.Length > 0
                && response.Candidates[0]?.Content?.Parts != null
                && response.Candidates[0].Content.Parts.Length > 0;
        }
    }
}