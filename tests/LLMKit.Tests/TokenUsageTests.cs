using LLMKit.Models;
using LLMKit.Providers;

namespace LLMKit.Tests
{
    public class TokenUsageTests
    {
        private static async Task<TokenUsage?> LastUsageAfterChat(ILLMProvider provider)
        {
            using var client = new LLMClient(provider);
            await client.GenerateTextAsync("hi");
            return client.LastUsage;
        }

        private static void AssertUsage(TokenUsage? usage, int input, int output, int? reasoning)
        {
            Assert.NotNull(usage);
            Assert.Equal(input, usage!.InputTokens);
            Assert.Equal(output, usage.OutputTokens);
            Assert.Equal(reasoning, usage.ReasoningTokens);
            Assert.Equal(input + output, usage.TotalTokens);
        }

        [Fact]
        public async Task OpenAI_ReadsUsageWithReasoningTokens()
        {
            var usage = await LastUsageAfterChat(new OpenAIResponseProbe("""
                {"choices":[{"message":{"content":"Hi"}}],
                 "usage":{"prompt_tokens":12,"completion_tokens":30,"total_tokens":42,
                          "completion_tokens_details":{"reasoning_tokens":20}}}
                """));

            AssertUsage(usage, input: 12, output: 30, reasoning: 20);
        }

        [Fact]
        public async Task OpenAI_EmptyAnswer_StillReportsUsage()
        {
            // All output tokens spent on reasoning: no text, but the usage explains why
            var response = await new OpenAIResponseProbe("""
                {"choices":[{"message":{"content":""}}],
                 "usage":{"prompt_tokens":12,"completion_tokens":10,"completion_tokens_details":{"reasoning_tokens":10}}}
                """).GenerateTextAsync(RequestJson.Hi(ReasoningEffort.High));

            Assert.Equal(string.Empty, response.Text);
            AssertUsage(response.Usage, input: 12, output: 10, reasoning: 10);
        }

        [Fact]
        public async Task OpenAI_NoUsageInResponse_IsNull()
        {
            var usage = await LastUsageAfterChat(new OpenAIResponseProbe("""{"choices":[{"message":{"content":"Hi"}}]}"""));

            Assert.Null(usage);
        }

        [Fact]
        public async Task Anthropic_AddsCachedTokensToInput_ReasoningNotReported()
        {
            var usage = await LastUsageAfterChat(new AnthropicResponseProbe("""
                {"content":[{"type":"thinking","thinking":"..."},{"type":"text","text":"Hi"}],
                 "usage":{"input_tokens":10,"output_tokens":25,"cache_creation_input_tokens":3,"cache_read_input_tokens":2}}
                """));

            AssertUsage(usage, input: 15, output: 25, reasoning: null);
        }

        [Fact]
        public async Task Gemini_AddsThoughtsToOutput()
        {
            var usage = await LastUsageAfterChat(new GeminiResponseProbe("""
                {"candidates":[{"content":{"parts":[{"text":"...","thought":true},{"text":"Hi"}]}}],
                 "usageMetadata":{"promptTokenCount":8,"candidatesTokenCount":5,"thoughtsTokenCount":40,"totalTokenCount":53}}
                """));

            AssertUsage(usage, input: 8, output: 45, reasoning: 40);
        }

        [Fact]
        public async Task Gemini_WithoutThinking_ReasoningIsNull()
        {
            var usage = await LastUsageAfterChat(new GeminiResponseProbe("""
                {"candidates":[{"content":{"parts":[{"text":"Hi"}]}}],
                 "usageMetadata":{"promptTokenCount":8,"candidatesTokenCount":5,"totalTokenCount":13}}
                """));

            AssertUsage(usage, input: 8, output: 5, reasoning: null);
        }

        [Fact]
        public async Task DeepSeek_ReadsUsageWithReasoningTokens()
        {
            var usage = await LastUsageAfterChat(new DeepSeekResponseProbe("""
                {"choices":[{"message":{"content":"Hi","reasoning_content":"..."}}],
                 "usage":{"prompt_tokens":9,"completion_tokens":50,"total_tokens":59,
                          "completion_tokens_details":{"reasoning_tokens":44}}}
                """));

            AssertUsage(usage, input: 9, output: 50, reasoning: 44);
        }

        [Fact]
        public async Task LLMStudio_WithoutDetails_ReasoningIsNull()
        {
            var usage = await LastUsageAfterChat(new LLMStudioResponseProbe("""
                {"choices":[{"message":{"content":"Hi"}}],
                 "usage":{"prompt_tokens":7,"completion_tokens":3,"total_tokens":10}}
                """));

            AssertUsage(usage, input: 7, output: 3, reasoning: null);
        }
    }
}
