namespace LLMKit.Tests
{
    public class PublicApiTests
    {
        private static readonly string[] PublicTypeNames = typeof(LLMClient).Assembly.GetExportedTypes()
            .Select(t => t.Name)
            .ToArray();

        [Theory]
        [InlineData("OpenAIResponse")]
        [InlineData("AnthropicResponse")]
        [InlineData("GeminiResponse")]
        [InlineData("DeepSeekResponse")]
        [InlineData("LLMStudioResponse")]
        [InlineData("ChatCompletionUsage")]
        [InlineData("Choice")]
        [InlineData("Message")]
        [InlineData("Candidate")]
        [InlineData("Content")]
        [InlineData("Part")]
        public void ProviderResponseModels_AreNotPublic(string typeName)
        {
            Assert.DoesNotContain(typeName, PublicTypeNames);
        }

        [Theory]
        [InlineData("LLMClient")]
        [InlineData("LLMResponse")]
        [InlineData("TokenUsage")]
        [InlineData("FinishReason")]
        [InlineData("ProviderCapabilities")]
        [InlineData("ReasoningEffort")]
        [InlineData("ILLMProvider")]
        public void ResultTypes_ArePublic(string typeName)
        {
            Assert.Contains(typeName, PublicTypeNames);
        }
    }
}
