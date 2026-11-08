using System.Text.Json;
using LLMKit.Models;

namespace LLMKit.Tests
{
    public class AnthropicReasoningTests
    {
        private static string? ThinkingType(JsonElement body) =>
            body.TryGetProperty("thinking", out var t) ? t.GetProperty("type").GetString() : null;

        private static int? Budget(JsonElement body) =>
            body.TryGetProperty("thinking", out var t) && t.TryGetProperty("budget_tokens", out var b) ? b.GetInt32() : null;

        private static string? Effort(JsonElement body) =>
            body.TryGetProperty("output_config", out var o) ? o.GetProperty("effort").GetString() : null;

        private static void AssertBody(JsonElement body, string? thinking, int? budget, string? effort, bool temperature)
        {
            Assert.Equal(thinking, ThinkingType(body));
            Assert.Equal(budget, Budget(body));
            Assert.Equal(effort, Effort(body));
            Assert.Equal(temperature, body.Has("temperature"));
        }

        [Theory]
        [InlineData("claude-opus-5-5")]
        [InlineData("claude-sonnet-5")]
        [InlineData("claude-fable-5-1")]
        [InlineData("claude-opus-4-7")]
        [InlineData("claude-opus-4-7[1m]")]
        [InlineData("claude-mythos-preview")]
        [InlineData("claude-2.1")]
        [InlineData("my-proxy-name")]
        public void AdaptiveModelsWithoutSampling(string model)
        {
            var probe = new AnthropicProbe(model);
            AssertBody(probe.Body(null), null, null, null, temperature: false);
            AssertBody(probe.Body(ReasoningEffort.None), null, null, "low", temperature: false);
            AssertBody(probe.Body(ReasoningEffort.Low), "adaptive", null, "low", temperature: false);
            AssertBody(probe.Body(ReasoningEffort.Medium), "adaptive", null, "medium", temperature: false);
            AssertBody(probe.Body(ReasoningEffort.High), "adaptive", null, "high", temperature: false);
        }

        [Theory]
        [InlineData("claude-opus-4-6")]
        [InlineData("claude-sonnet-4-6")]
        public void Claude46_DropsTemperatureOnlyWhileThinking(string model)
        {
            var probe = new AnthropicProbe(model);
            AssertBody(probe.Body(null), null, null, null, temperature: true);
            AssertBody(probe.Body(ReasoningEffort.None), null, null, "low", temperature: true);
            AssertBody(probe.Body(ReasoningEffort.Low), "adaptive", null, "low", temperature: false);
            AssertBody(probe.Body(ReasoningEffort.Medium), "adaptive", null, "medium", temperature: false);
            AssertBody(probe.Body(ReasoningEffort.High), "adaptive", null, "high", temperature: false);
        }

        [Theory]
        [InlineData("claude-opus-4-5")]
        [InlineData("claude-opus-4-5-20251101")]
        public void Opus45_UsesManualThinkingAndKeepsEffort(string model)
        {
            var probe = new AnthropicProbe(model);
            AssertBody(probe.Body(null), null, null, null, temperature: true);
            AssertBody(probe.Body(ReasoningEffort.None), null, null, "low", temperature: true);
            AssertBody(probe.Body(ReasoningEffort.Low), "enabled", 1024, "low", temperature: false);
            AssertBody(probe.Body(ReasoningEffort.Medium), "enabled", 3072, "medium", temperature: false);
            AssertBody(probe.Body(ReasoningEffort.High), "enabled", 3072, "high", temperature: false);
        }

        [Theory]
        [InlineData("claude-sonnet-4-5-20250929")]
        [InlineData("claude-haiku-4-5")]
        [InlineData("claude-opus-4-20250514")]
        [InlineData("claude-opus-4-1")]
        [InlineData("claude-3-7-sonnet-20250219")]
        public void ManualThinkingModels_UseBudgetWithoutEffort(string model)
        {
            var probe = new AnthropicProbe(model);
            AssertBody(probe.Body(null), null, null, null, temperature: true);
            AssertBody(probe.Body(ReasoningEffort.None), null, null, null, temperature: true);
            AssertBody(probe.Body(ReasoningEffort.Low), "enabled", 1024, null, temperature: false);
            AssertBody(probe.Body(ReasoningEffort.Medium), "enabled", 3072, null, temperature: false);
            AssertBody(probe.Body(ReasoningEffort.High), "enabled", 3072, null, temperature: false);
        }

        [Theory]
        [InlineData("claude-3-5-sonnet-20241022")]
        [InlineData("claude-3-opus-20240229")]
        [InlineData("claude-instant-1.2")]
        public void ModelsWithoutThinking_SendTemperatureOnly(string model)
        {
            var probe = new AnthropicProbe(model);
            foreach (var effort in new ReasoningEffort?[] { null, ReasoningEffort.None, ReasoningEffort.Low, ReasoningEffort.Medium, ReasoningEffort.High })
                AssertBody(probe.Body(effort), null, null, null, temperature: true);
        }

        [Theory]
        [InlineData(2048, ReasoningEffort.Low, 1024)]
        [InlineData(2047, ReasoningEffort.Low, null)]
        [InlineData(1500, ReasoningEffort.High, null)]
        public void ManualBudget_KeepsRoomForTheAnswer(int maxTokens, ReasoningEffort effort, int? expectedBudget)
        {
            var body = new AnthropicProbe("claude-sonnet-4-5").Body(effort, maxTokens);

            Assert.Equal(expectedBudget, Budget(body));
            // Without thinking the request is still valid and keeps the temperature
            Assert.Equal(expectedBudget == null, body.Has("temperature"));
        }

        [Fact]
        public void Temperature_IsCappedAtOne()
        {
            var request = RequestJson.Hi(null).WithTemperature(1.7);
            var body = new AnthropicProbe("claude-sonnet-4-5").BodyOf(request);

            Assert.Equal(1.0, body.GetProperty("temperature").GetDouble());
        }

        [Fact]
        public void Timeout_IsThreeMinutes()
        {
            Assert.Equal(TimeSpan.FromMinutes(3), new AnthropicProbe("claude-opus-5-5").Timeout);
        }
    }
}
