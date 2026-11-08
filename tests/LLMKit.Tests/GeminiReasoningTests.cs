using System.Text.Json;
using LLMKit.Models;

namespace LLMKit.Tests
{
    public class GeminiReasoningTests
    {
        private static readonly ReasoningEffort[] Levels =
            { ReasoningEffort.None, ReasoningEffort.Low, ReasoningEffort.Medium, ReasoningEffort.High };

        private static JsonElement? ThinkingConfig(JsonElement body) =>
            body.GetProperty("generation_config").TryGetProperty("thinking_config", out var t) ? t : null;

        private static void AssertLevels(string model, params string[] expected)
        {
            var probe = new GeminiProbe(model);
            for (int i = 0; i < Levels.Length; i++)
            {
                var config = ThinkingConfig(probe.Body(Levels[i]));
                Assert.NotNull(config);
                Assert.False(config.Value.Has("thinking_budget"));
                Assert.Equal(expected[i], config.Value.Str("thinking_level"));
                Assert.Equal(Levels[i] != ReasoningEffort.None, config.Value.Has("include_thoughts"));
            }
        }

        private static void AssertBudgets(string model, params int[] expected)
        {
            var probe = new GeminiProbe(model);
            for (int i = 0; i < Levels.Length; i++)
            {
                var config = ThinkingConfig(probe.Body(Levels[i]));
                Assert.NotNull(config);
                Assert.False(config.Value.Has("thinking_level"));
                Assert.Equal(expected[i], config.Value.GetProperty("thinking_budget").GetInt32());
                Assert.Equal(Levels[i] != ReasoningEffort.None, config.Value.Has("include_thoughts"));
            }
        }

        [Theory]
        [InlineData("gemini-2.5-flash")]
        [InlineData("gemini-2.5-flash-lite")]
        public void Gemini25Flash_UsesBudgets(string model) => AssertBudgets(model, 0, 1024, 8192, 24576);

        [Fact]
        public void Gemini25Pro_CannotTurnThinkingOff() => AssertBudgets("gemini-2.5-pro", 128, 1024, 8192, 24576);

        [Theory]
        [InlineData("gemini-3-flash-preview")]
        [InlineData("gemini-3.1-flash-lite")]
        [InlineData("gemini-3.5-flash-lite")]
        [InlineData("gemini-3.6-flash")]
        public void AllLevelModels(string model) => AssertLevels(model, "minimal", "low", "medium", "high");

        [Theory]
        [InlineData("gemini-3.1-pro-preview")]
        [InlineData("gemini-3.8-flash")]
        [InlineData("gemini-4-argon")]
        [InlineData("gemini-flash-latest")]
        [InlineData("gemini-5-anything")]       // hypothetical future generation
        public void ModelsWithoutMinimal_SendLowForNone(string model) => AssertLevels(model, "low", "low", "medium", "high");

        [Fact]
        public void Gemini3ProPreview_HasNoMedium() => AssertLevels("gemini-3-pro-preview", "low", "low", "high", "high");

        [Theory]
        [InlineData("gemini-3.1-flash-lite-image")]
        [InlineData("gemini-3.1-flash-image")]
        [InlineData("gemma-4-31b-it")]
        public void MinimalOrHighModels(string model) => AssertLevels(model, "minimal", "high", "high", "high");

        [Fact]
        public void Gemini3ImageModels_StillThink()
        {
            // Only the 2.5 image models lack thinking
            Assert.NotNull(ThinkingConfig(new GeminiProbe("gemini-3.1-flash-image").Body(ReasoningEffort.High)));
            Assert.True(new GeminiProbe("gemini-3.1-flash-image").GetCapabilities(RequestJson.Hi(ReasoningEffort.High)).SupportsReasoningEffort);
            Assert.False(new GeminiProbe("gemini-2.5-flash-image").GetCapabilities(RequestJson.Hi(ReasoningEffort.High)).SupportsReasoningEffort);
        }

        [Fact]
        public void Gemini3ProImage_IsHighOnly() => AssertLevels("gemini-3-pro-image", "high", "high", "high", "high");

        [Fact]
        public void NanoBanana_HasNoLow() => AssertLevels("gemini-nano-banana-2.1", "minimal", "medium", "medium", "high");

        [Theory]
        [InlineData("gemini-2.0-flash")]
        [InlineData("gemini-1.5-pro")]
        [InlineData("gemma-3-27b-it")]
        [InlineData("gemini-2.5-flash-image")]          // models list: no thinking
        [InlineData("gemini-2.5-flash-preview-tts")]
        [InlineData("gemini-2.5-pro-preview-tts")]
        [InlineData("gemini-3.1-flash-tts-preview")]
        [InlineData("gemini-3.8-flash-tts")]
        public void ModelsWithoutThinking_SendNoThinkingConfig(string model)
        {
            var probe = new GeminiProbe(model);
            foreach (var effort in Levels)
                Assert.Null(ThinkingConfig(probe.Body(effort)));
        }

        [Theory]
        [InlineData("gemini-2.5-flash")]
        [InlineData("gemini-3.6-flash")]
        [InlineData("gemma-4-31b-it")]
        public void NoEffort_SendsNoThinkingConfig(string model)
        {
            Assert.Null(ThinkingConfig(new GeminiProbe(model).Body(null)));
        }
    }
}
