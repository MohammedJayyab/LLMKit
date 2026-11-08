namespace LLMKit.Tests
{
    public class SupportedProvidersTests
    {
        [Fact]
        public void GetSupportedProviders_ListsEveryImplementedProvider()
        {
            Assert.Equal(
                new[] { "Anthropic", "DeepSeek", "Gemini", "LLMStudio", "OpenAI" },
                LLMClient.GetSupportedProviders());
        }
    }
}
