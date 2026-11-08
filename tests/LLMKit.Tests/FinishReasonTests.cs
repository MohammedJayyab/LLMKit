using LLMKit.Models;
using LLMKit.Providers;

namespace LLMKit.Tests
{
    public class FinishReasonTests
    {
        private static async Task<(string Text, FinishReason? Reason)> Chat(ILLMProvider provider)
        {
            using var client = new LLMClient(provider);
            string text = await client.GenerateTextAsync("hi");
            return (text, client.LastFinishReason);
        }

        [Theory]
        [InlineData("stop", FinishReason.Completed)]
        [InlineData("length", FinishReason.MaxTokens)]
        [InlineData("content_filter", FinishReason.ContentFilter)]
        [InlineData("tool_calls", FinishReason.Other)]
        public async Task OpenAI_MapsFinishReason(string value, FinishReason expected)
        {
            var (_, reason) = await Chat(new OpenAIResponseProbe(
                $$"""{"choices":[{"message":{"content":"Hi"},"finish_reason":"{{value}}"}]}"""));

            Assert.Equal(expected, reason);
        }

        [Fact]
        public async Task OpenAI_EmptyAnswer_ReportsMaxTokens()
        {
            // The reasoning used all the tokens: no text, but the reason explains it
            var (text, reason) = await Chat(new OpenAIResponseProbe("""
                {"choices":[{"message":{"content":""},"finish_reason":"length"}],
                 "usage":{"prompt_tokens":12,"completion_tokens":50,"completion_tokens_details":{"reasoning_tokens":50}}}
                """));

            Assert.Equal(string.Empty, text);
            Assert.Equal(FinishReason.MaxTokens, reason);
        }

        [Fact]
        public async Task NotReported_IsNull()
        {
            var (_, reason) = await Chat(new OpenAIResponseProbe("""{"choices":[{"message":{"content":"Hi"}}]}"""));

            Assert.Null(reason);
        }

        [Theory]
        [InlineData("end_turn", FinishReason.Completed)]
        [InlineData("stop_sequence", FinishReason.Completed)]
        [InlineData("max_tokens", FinishReason.MaxTokens)]
        [InlineData("refusal", FinishReason.ContentFilter)]
        [InlineData("tool_use", FinishReason.Other)]
        public async Task Anthropic_MapsStopReason(string value, FinishReason expected)
        {
            var (_, reason) = await Chat(new AnthropicResponseProbe(
                $$"""{"content":[{"type":"text","text":"Hi"}],"stop_reason":"{{value}}"}"""));

            Assert.Equal(expected, reason);
        }

        [Theory]
        [InlineData("STOP", FinishReason.Completed)]
        [InlineData("MAX_TOKENS", FinishReason.MaxTokens)]
        [InlineData("SAFETY", FinishReason.ContentFilter)]
        [InlineData("RECITATION", FinishReason.ContentFilter)]
        [InlineData("PROHIBITED_CONTENT", FinishReason.ContentFilter)]
        [InlineData("LANGUAGE", FinishReason.Other)]
        public async Task Gemini_MapsFinishReason(string value, FinishReason expected)
        {
            var (_, reason) = await Chat(new GeminiResponseProbe(
                $$"""{"candidates":[{"content":{"parts":[{"text":"Hi"}]},"finishReason":"{{value}}"}]}"""));

            Assert.Equal(expected, reason);
        }

        [Fact]
        public async Task Gemini_ThinkingUsedAllTokens_EmptyWithMaxTokens()
        {
            // Thinking used all the tokens: no answer parts, only the finish reason and the usage
            var (text, reason) = await Chat(new GeminiResponseProbe("""
                {"candidates":[{"content":{"role":"model"},"finishReason":"MAX_TOKENS"}],
                 "usageMetadata":{"promptTokenCount":13,"thoughtsTokenCount":7}}
                """));

            Assert.Equal(string.Empty, text);
            Assert.Equal(FinishReason.MaxTokens, reason);
        }

        [Fact]
        public async Task Gemini_BlockedPrompt_IsContentFilter()
        {
            var (text, reason) = await Chat(new GeminiResponseProbe("""{"promptFeedback":{"blockReason":"SAFETY"}}"""));

            Assert.Equal(string.Empty, text);
            Assert.Equal(FinishReason.ContentFilter, reason);
        }

        [Fact]
        public async Task DeepSeek_And_LLMStudio_MapFinishReason()
        {
            var (_, deepSeek) = await Chat(new DeepSeekResponseProbe(
                """{"choices":[{"message":{"content":"Hi"},"finish_reason":"length"}]}"""));
            var (_, llmStudio) = await Chat(new LLMStudioResponseProbe(
                """{"choices":[{"message":{"content":"Hi"},"finish_reason":"stop"}]}"""));

            Assert.Equal(FinishReason.MaxTokens, deepSeek);
            Assert.Equal(FinishReason.Completed, llmStudio);
        }

        [Fact]
        public async Task LLMClient_EmptyAnswer_IsNotStoredInTheConversation()
        {
            // Anthropic and Gemini reject an empty assistant message, so it must not reach the next request
            var provider = new OpenAIResponseProbe("""{"choices":[{"message":{"content":""},"finish_reason":"length"}]}""");
            using var client = new LLMClient(provider);

            Assert.Equal(string.Empty, await client.GenerateTextAsync("first question"));
            Assert.Equal(2, client.CurrentConversation.MessageCount); // system + question, no empty answer

            provider.RespondWith("""{"choices":[{"message":{"content":"Second answer"},"finish_reason":"stop"}]}""");
            Assert.Equal("Second answer", await client.GenerateTextAsync("second question"));

            using var sent = System.Text.Json.JsonDocument.Parse(provider.LastRequestBody!);
            var roles = sent.RootElement.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("role").GetString()).ToArray();
            Assert.Equal(new[] { "system", "user", "user" }, roles);
            Assert.Equal(4, client.CurrentConversation.MessageCount); // + second question and its answer
        }

        [Fact]
        public async Task LLMClient_FailedCall_ClearsTheLastResponseValues()
        {
            var provider = new OpenAIResponseProbe("""
                {"choices":[{"message":{"content":"Hi"},"finish_reason":"length"}],
                 "usage":{"prompt_tokens":12,"completion_tokens":30}}
                """);
            using var client = new LLMClient(provider);

            await client.GenerateTextAsync("hi");
            Assert.Equal(FinishReason.MaxTokens, client.LastFinishReason);
            Assert.NotNull(client.LastUsage);

            provider.FailWith(System.Net.HttpStatusCode.BadRequest, """{"error":{"message":"max_tokens is too large"}}""");
            await Assert.ThrowsAsync<LLMKit.Exceptions.LLMException>(() => client.GenerateTextAsync("hi again"));

            // The failed call must not report the previous call's values
            Assert.Null(client.LastFinishReason);
            Assert.Null(client.LastUsage);
            Assert.Null(client.LastReasoning);
        }

        [Fact]
        public async Task LLMClient_LastFinishReason_FollowsTheLastResponse()
        {
            var provider = new OpenAIResponseProbe("""{"choices":[{"message":{"content":"Hi"},"finish_reason":"length"}]}""");
            using var client = new LLMClient(provider);

            Assert.Null(client.LastFinishReason);
            await client.GenerateTextAsync("hi");
            Assert.Equal(FinishReason.MaxTokens, client.LastFinishReason);
        }
    }
}
