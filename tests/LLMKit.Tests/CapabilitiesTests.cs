using System.Text.Json;
using LLMKit.Models;
using LLMKit.Providers;

namespace LLMKit.Tests
{
    public class CapabilitiesTests
    {
        private static readonly ReasoningEffort?[] Efforts =
            { null, ReasoningEffort.None, ReasoningEffort.Low, ReasoningEffort.Medium, ReasoningEffort.High };

        private static ProviderCapabilities Caps(ILLMProvider provider, ReasoningEffort? effort, int maxTokens = RequestJson.DefaultMaxTokens) =>
            provider.GetCapabilities(RequestJson.Hi(effort, maxTokens));

        // The capabilities must match what is actually sent

        [Theory]
        [InlineData("gpt-3.5-turbo")]
        [InlineData("gpt-4o")]
        [InlineData("gpt-5")]
        [InlineData("gpt-5.1")]
        [InlineData("gpt-5.1-chat-latest")]
        [InlineData("gpt-6-luna")]
        [InlineData("gpt-6-astra")]
        [InlineData("o3")]
        public void OpenAI_MatchesRequestBody(string model)
        {
            var probe = new OpenAIProbe(model);
            foreach (var effort in Efforts)
            {
                var caps = Caps(probe, effort);
                var body = probe.Body(effort);
                Assert.Equal(body.Has("temperature"), caps.SupportsTemperature);
                if (effort == ReasoningEffort.Low)
                    Assert.Equal(body.Has("reasoning_effort"), caps.SupportsReasoningEffort);
            }
        }

        [Theory]
        [InlineData("claude-3-5-sonnet-20241022")]
        [InlineData("claude-3-7-sonnet-20250219")]
        [InlineData("claude-sonnet-4-5")]
        [InlineData("claude-opus-4-5")]
        [InlineData("claude-opus-4-6")]
        [InlineData("claude-opus-5-5")]
        [InlineData("claude-haiku-5-5")]
        public void Anthropic_MatchesRequestBody(string model)
        {
            var probe = new AnthropicProbe(model);
            foreach (int maxTokens in new[] { RequestJson.DefaultMaxTokens, 1500 }) // 1500: no room for a manual budget
            {
                foreach (var effort in Efforts)
                {
                    var caps = Caps(probe, effort, maxTokens);
                    var body = probe.Body(effort, maxTokens);
                    Assert.Equal(body.Has("temperature"), caps.SupportsTemperature);
                    Assert.Equal(body.Has("thinking"), caps.ReturnsReasoningText);
                }

                // The effort is sent as thinking or output_config.effort, or not at all
                var low = probe.Body(ReasoningEffort.Low, maxTokens);
                Assert.Equal(low.Has("thinking") || low.Has("output_config"),
                    Caps(probe, ReasoningEffort.Low, maxTokens).SupportsReasoningEffort);
            }
        }

        [Theory]
        [InlineData("gemini-2.0-flash")]
        [InlineData("gemini-2.5-flash")]
        [InlineData("gemini-2.5-pro")]
        [InlineData("gemini-3-pro")]
        [InlineData("gemini-3.6-flash")]
        [InlineData("gemma-3-27b-it")]
        [InlineData("gemma-4-27b-it")]
        public void Gemini_MatchesRequestBody(string model)
        {
            var probe = new GeminiProbe(model);
            foreach (var effort in Efforts)
            {
                var caps = Caps(probe, effort);
                var config = probe.Body(effort).GetProperty("generation_config");
                Assert.Equal(config.Has("temperature"), caps.SupportsTemperature);
                bool includesThoughts = config.TryGetProperty("thinking_config", out var thinking) && thinking.Has("include_thoughts");
                // Gemma 4 returns its thoughts by default, without include_thoughts (seen on the live API)
                bool gemma4Default = effort == null && model.StartsWith("gemma-4", StringComparison.Ordinal);
                Assert.Equal(includesThoughts || gemma4Default, caps.ReturnsReasoningText);
                if (effort == ReasoningEffort.Low)
                    Assert.Equal(config.Has("thinking_config"), caps.SupportsReasoningEffort);
            }
        }

        // Expected values for typical models

        [Fact]
        public void OpenAI_ReasoningModel_TemperatureOnlyWithReasoningOff()
        {
            var probe = new OpenAIProbe("gpt-6-luna");
            Assert.True(Caps(probe, ReasoningEffort.Low).SupportsReasoningEffort);
            Assert.True(Caps(probe, ReasoningEffort.Low).CanTurnOffReasoning);
            Assert.False(Caps(probe, ReasoningEffort.Low).SupportsTemperature);
            Assert.True(Caps(probe, ReasoningEffort.None).SupportsTemperature);
            Assert.False(Caps(probe, ReasoningEffort.High).ReturnsReasoningText);
        }

        [Theory]
        [InlineData("gpt-6-astra")]
        [InlineData("o3")]
        [InlineData("gpt-5")]
        public void OpenAI_AlwaysOnReasoning_CannotTurnOff(string model)
        {
            var caps = Caps(new OpenAIProbe(model), ReasoningEffort.None);
            Assert.True(caps.SupportsReasoningEffort);
            Assert.False(caps.CanTurnOffReasoning);
            Assert.False(caps.SupportsTemperature);
        }

        [Fact]
        public void OpenAI_RegularModel_NoReasoning()
        {
            var caps = Caps(new OpenAIProbe("gpt-4o"), ReasoningEffort.High);
            Assert.False(caps.SupportsReasoningEffort);
            Assert.False(caps.CanTurnOffReasoning);
            Assert.True(caps.SupportsTemperature);
            Assert.True(caps.SupportsImages);
        }

        [Fact]
        public void Anthropic_Values()
        {
            var opus = new AnthropicProbe("claude-opus-5-5");
            Assert.True(Caps(opus, ReasoningEffort.Low).SupportsReasoningEffort);
            Assert.True(Caps(opus, ReasoningEffort.Low).ReturnsReasoningText);
            Assert.False(Caps(opus, ReasoningEffort.None).ReturnsReasoningText);
            Assert.False(Caps(opus, ReasoningEffort.None).SupportsTemperature); // Claude 4.7 and later reject it

            var old = new AnthropicProbe("claude-3-5-sonnet-20241022");
            Assert.False(Caps(old, ReasoningEffort.High).SupportsReasoningEffort);
            Assert.True(Caps(old, ReasoningEffort.High).SupportsTemperature);
        }

        [Theory]
        [InlineData("gemini-2.5-flash", true)]
        [InlineData("gemini-2.5-pro", false)]   // minimum budget 128
        [InlineData("gemini-3-pro", false)]     // lowest level still thinks
        [InlineData("gemini-3.6-flash", false)]
        [InlineData("gemma-4-27b-it", true)]    // "minimal" turns thinking off
        [InlineData("gemini-2.0-flash", false)] // does not think
        public void Gemini_CanTurnOffReasoning(string model, bool expected)
        {
            Assert.Equal(expected, Caps(new GeminiProbe(model), ReasoningEffort.None).CanTurnOffReasoning);
        }

        [Theory]
        [InlineData("gemma-4-31b-it")]
        [InlineData("gemma-4-26b-a4b-it")]
        public void Gemma4_ReturnsThoughtsByDefault_NotWithNone(string model)
        {
            var probe = new GeminiProbe(model);
            Assert.True(Caps(probe, null).ReturnsReasoningText);
            Assert.False(Caps(probe, ReasoningEffort.None).ReturnsReasoningText);
            Assert.True(Caps(probe, ReasoningEffort.Low).ReturnsReasoningText);
        }

        [Fact]
        public void DeepSeek_TemperatureOnlyWithThinkingOff_NoImages()
        {
            var probe = new DeepSeekProbe("deepseek-flash");
            Assert.False(Caps(probe, null).SupportsTemperature); // thinks by default
            Assert.True(Caps(probe, null).ReturnsReasoningText);
            Assert.True(Caps(probe, ReasoningEffort.None).SupportsTemperature);
            Assert.False(Caps(probe, ReasoningEffort.None).ReturnsReasoningText);
            Assert.True(Caps(probe, ReasoningEffort.None).CanTurnOffReasoning);
            Assert.False(Caps(probe, ReasoningEffort.High).SupportsImages);
        }

        [Fact]
        public void LLMStudio_ReportsWhatIsSent()
        {
            var caps = Caps(new LLMStudioProbe("glm-4.6v-flash"), ReasoningEffort.Low);
            Assert.True(caps.SupportsReasoningEffort);
            Assert.True(caps.SupportsTemperature);
            Assert.True(caps.SupportsImages);
        }

        // LLMClient and custom providers

        [Fact]
        public void LLMClient_Capabilities_UseTheClientSettings()
        {
            using var thinking = new LLMClient(new OpenAIProbe("gpt-6-luna"), reasoningEffort: ReasoningEffort.Low);
            using var notThinking = new LLMClient(new OpenAIProbe("gpt-6-luna"), reasoningEffort: ReasoningEffort.None);

            Assert.False(thinking.Capabilities.SupportsTemperature);
            Assert.True(notThinking.Capabilities.SupportsTemperature);
        }

        [Fact]
        public void LLMClient_Capabilities_UseTheClientMaxTokens()
        {
            // Claude 3.7 needs room for a 1024-token budget plus the answer
            using var roomy = new LLMClient(new AnthropicProbe("claude-3-7-sonnet-20250219"), maxTokens: 4096, reasoningEffort: ReasoningEffort.Low);
            using var tight = new LLMClient(new AnthropicProbe("claude-3-7-sonnet-20250219"), maxTokens: 1500, reasoningEffort: ReasoningEffort.Low);

            Assert.True(roomy.Capabilities.ReturnsReasoningText);
            Assert.False(tight.Capabilities.ReturnsReasoningText);
            Assert.True(tight.Capabilities.SupportsTemperature);
        }

        [Fact]
        public void CustomProvider_WithoutOverride_ReportsNoCapabilities()
        {
            using var client = new LLMClient(new CustomProvider());
            var caps = client.Capabilities;

            Assert.False(caps.SupportsReasoningEffort);
            Assert.False(caps.CanTurnOffReasoning);
            Assert.False(caps.SupportsTemperature);
            Assert.False(caps.ReturnsReasoningText);
            Assert.False(caps.SupportsImages);
        }

        private sealed class CustomProvider : ILLMProvider
        {
            public Uri Endpoint { get; } = new("https://example.com");
            public Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default) =>
                Task.FromResult(new LLMResponse { Text = "ok" });
            public void Dispose() { }
        }
    }
}
