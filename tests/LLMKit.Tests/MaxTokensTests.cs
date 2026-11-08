using System.Text.Json;
using LLMKit.Models;

namespace LLMKit.Tests
{
    public class MaxTokensTests
    {
        private const int AboveOldCap = 16000;

        private static LLMRequest Request(int maxTokens) => RequestJson.Hi(null, maxTokens);

        [Theory]
        [InlineData(16000, 16000)]   // above the old 4096 cap: sent as set
        [InlineData(128000, 128000)]
        [InlineData(4096, 4096)]
        [InlineData(1000, 1000)]
        [InlineData(0, 1)]           // below 1: raised to 1
        [InlineData(-5, 1)]
        public void WithMaxTokens_KeepsTheValue_WithoutUpperLimit(int value, int expected)
        {
            Assert.Equal(expected, new LLMRequest().WithMaxTokens(value).MaxTokens);
        }

        [Fact]
        public void WithMaxTokens_Null_UsesTheDefault()
        {
            Assert.Equal(RequestJson.DefaultMaxTokens, new LLMRequest().WithMaxTokens(null).MaxTokens);
            Assert.Equal(RequestJson.DefaultMaxTokens, new LLMRequest().MaxTokens);
        }

        [Fact]
        public void EveryProvider_SendsTheValueAboveTheOldCap()
        {
            Assert.Equal(AboveOldCap, new OpenAIProbe("gpt-6-luna").BodyOf(Request(AboveOldCap))
                .GetProperty("max_completion_tokens").GetInt32());
            Assert.Equal(AboveOldCap, new AnthropicProbe("claude-opus-5-5").BodyOf(Request(AboveOldCap))
                .GetProperty("max_tokens").GetInt32());
            Assert.Equal(AboveOldCap, new GeminiProbe("gemini-3.6-flash").BodyOf(Request(AboveOldCap))
                .GetProperty("generation_config").GetProperty("max_output_tokens").GetInt32());
            Assert.Equal(AboveOldCap, new DeepSeekProbe("deepseek-flash").BodyOf(Request(AboveOldCap))
                .GetProperty("max_tokens").GetInt32());
            Assert.Equal(AboveOldCap, new LLMStudioProbe("glm-4.6v-flash").BodyOf(Request(AboveOldCap))
                .GetProperty("max_tokens").GetInt32());
        }

        [Theory]
        [InlineData(32000, ReasoningEffort.High, 24576)]   // the full High budget fits now
        [InlineData(16000, ReasoningEffort.High, 14976)]   // kept 1024 below max_tokens
        [InlineData(16000, ReasoningEffort.Medium, 8192)]
        [InlineData(4096, ReasoningEffort.High, 3072)]     // the default, unchanged
        public void Anthropic_ManualBudget_UsesTheHigherLimit(int maxTokens, ReasoningEffort effort, int expectedBudget)
        {
            var body = new AnthropicProbe("claude-sonnet-4-5").Body(effort, maxTokens);

            Assert.Equal(expectedBudget, body.GetProperty("thinking").GetProperty("budget_tokens").GetInt32());
        }

        [Fact]
        public async Task LLMClient_SendsTheConfiguredValue()
        {
            var provider = new OpenAIResponseProbe("""{"choices":[{"message":{"content":"Hi"}}]}""");
            using var client = new LLMClient(provider, maxTokens: AboveOldCap);

            await client.GenerateTextAsync("hi");

            using var sent = JsonDocument.Parse(provider.LastRequestBody!);
            Assert.Equal(AboveOldCap, sent.RootElement.GetProperty("max_completion_tokens").GetInt32());
        }
    }
}
