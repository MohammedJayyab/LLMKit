using System.Text.Json;
using System.Text.Json.Serialization;
using LLMKit.Models;
using LLMKit.Providers;

namespace LLMKit.Tests
{
    /// <summary>
    /// Builds requests and serializes request bodies the same way the providers do (nulls omitted).
    /// </summary>
    internal static class RequestJson
    {
        /// <summary>LLMKit's default and maximum max_tokens.</summary>
        public const int DefaultMaxTokens = 4096;

        private static readonly JsonSerializerOptions Options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static LLMRequest Hi(ReasoningEffort? effort, int maxTokens = RequestJson.DefaultMaxTokens) =>
            new LLMRequest()
                .WithMessages(new[] { new ChatMessage(ChatMessage.Roles.User, "hi") })
                .WithMaxTokens(maxTokens)
                .WithReasoningEffort(effort);

        public static JsonElement Serialize(object data) => JsonSerializer.SerializeToElement(data, Options);

        public static bool Has(this JsonElement body, string name) => body.TryGetProperty(name, out _);

        public static string? Str(this JsonElement body, string name) =>
            body.TryGetProperty(name, out var value) ? value.GetString() : null;
    }

    // Probe subclasses expose the protected request building; no network is used.

    internal sealed class OpenAIProbe : OpenAIProvider
    {
        public OpenAIProbe(string model) : base("test-key", model) { }
        public OpenAIProbe(string model, bool isReasoningModel) : base("test-key", model, null, isReasoningModel) { }
        public TimeSpan Timeout => HttpClient.Timeout;
        public JsonElement Body(ReasoningEffort? effort) => RequestJson.Serialize(CreateRequestData(RequestJson.Hi(effort)));
        public JsonElement BodyOf(LLMRequest request) => RequestJson.Serialize(CreateRequestData(request));
    }

    internal sealed class AnthropicProbe : AnthropicProvider
    {
        public AnthropicProbe(string model) : base("test-key", model) { }
        public TimeSpan Timeout => HttpClient.Timeout;
        public JsonElement Body(ReasoningEffort? effort, int maxTokens = RequestJson.DefaultMaxTokens) =>
            RequestJson.Serialize(CreateRequestData(RequestJson.Hi(effort, maxTokens)));
        public JsonElement BodyOf(LLMRequest request) => RequestJson.Serialize(CreateRequestData(request));
    }

    internal sealed class GeminiProbe : GeminiProvider
    {
        public GeminiProbe(string model) : base("test-key", model) { }
        public TimeSpan Timeout => HttpClient.Timeout;
        public JsonElement Body(ReasoningEffort? effort) => RequestJson.Serialize(CreateRequestData(RequestJson.Hi(effort)));
        public JsonElement BodyOf(LLMRequest request) => RequestJson.Serialize(CreateRequestData(request));
    }

    internal sealed class DeepSeekProbe : DeepSeekProvider
    {
        public DeepSeekProbe(string model) : base("test-key", model) { }
        public TimeSpan Timeout => HttpClient.Timeout;
        public JsonElement Body(ReasoningEffort? effort) => RequestJson.Serialize(CreateRequestData(RequestJson.Hi(effort)));
        public JsonElement BodyOf(LLMRequest request) => RequestJson.Serialize(CreateRequestData(request));
    }

    internal sealed class LLMStudioProbe : LLMStudioProvider
    {
        public LLMStudioProbe(string model, string? apiKey = null) : base(apiKey, model) { }
        public System.Net.Http.Headers.AuthenticationHeaderValue? Authorization => HttpClient.DefaultRequestHeaders.Authorization;
        public JsonElement Body(ReasoningEffort? effort) => RequestJson.Serialize(CreateRequestData(RequestJson.Hi(effort)));
        public JsonElement BodyOf(LLMRequest request) => RequestJson.Serialize(CreateRequestData(request));
    }
}
