using LLMKit.Models;

namespace LLMKit.Tests
{
    public class DeepSeekAndLLMStudioReasoningTests
    {
        private static string? ThinkingType(System.Text.Json.JsonElement body) =>
            body.TryGetProperty("thinking", out var t) ? t.GetProperty("type").GetString() : null;

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(ReasoningEffort.None, "disabled", null)]
        [InlineData(ReasoningEffort.Low, "enabled", "low")]
        [InlineData(ReasoningEffort.Medium, "enabled", "medium")]
        [InlineData(ReasoningEffort.High, "enabled", "high")]
        public void DeepSeek_MapsEffortToThinking(ReasoningEffort? effort, string? thinking, string? reasoningEffort)
        {
            var body = new DeepSeekProbe("deepseek-flash").Body(effort);

            Assert.Equal(thinking, ThinkingType(body));
            Assert.Equal(reasoningEffort, body.Str("reasoning_effort"));
            Assert.True(body.Has("temperature"));
        }

        [Fact]
        public void DeepSeek_TimeoutIsThreeMinutes()
        {
            Assert.Equal(TimeSpan.FromMinutes(3), new DeepSeekProbe("deepseek-flash").Timeout);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(ReasoningEffort.None, "none")]
        [InlineData(ReasoningEffort.Low, "low")]
        [InlineData(ReasoningEffort.Medium, "medium")]
        [InlineData(ReasoningEffort.High, "high")]
        public void LLMStudio_SendsEffortUnchanged(ReasoningEffort? effort, string? reasoningEffort)
        {
            var body = new LLMStudioProbe("local-model").Body(effort);

            Assert.Equal(reasoningEffort, body.Str("reasoning_effort"));
            Assert.True(body.Has("temperature"));
        }

        [Fact]
        public void LLMStudio_SendsAuthorizationWhenApiKeyIsGiven()
        {
            var auth = new LLMStudioProbe("local-model", "my-key").Authorization;

            Assert.NotNull(auth);
            Assert.Equal("Bearer", auth.Scheme);
            Assert.Equal("my-key", auth.Parameter);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void LLMStudio_SendsNoAuthorizationWithoutApiKey(string? apiKey)
        {
            Assert.Null(new LLMStudioProbe("local-model", apiKey).Authorization);
        }
    }
}
