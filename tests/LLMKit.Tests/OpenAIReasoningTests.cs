using LLMKit.Models;

namespace LLMKit.Tests
{
    public class OpenAIReasoningTests
    {
        private static readonly ReasoningEffort?[] AllEfforts =
            { null, ReasoningEffort.None, ReasoningEffort.Low, ReasoningEffort.Medium, ReasoningEffort.High };

        private static void AssertBody(OpenAIProbe probe, ReasoningEffort? effort, string? expectedEffort, bool expectSampling)
        {
            var body = probe.Body(effort);

            Assert.Equal(expectedEffort, body.Str("reasoning_effort"));
            Assert.Equal(expectSampling, body.Has("temperature"));
            Assert.Equal(expectSampling, body.Has("top_p"));
            Assert.Equal(expectSampling, body.Has("frequency_penalty"));
            Assert.Equal(expectSampling, body.Has("presence_penalty"));
        }

        [Theory]
        [InlineData("gpt-4o")]
        [InlineData("gpt-4.1")]
        [InlineData("gpt-3.5-turbo")]
        [InlineData("gpt-oss-120b")]
        [InlineData("chatgpt-4o-latest")]
        [InlineData("gpt-5-chat-latest")]
        [InlineData("gpt-6-chat")]              // hypothetical: tests the gpt-<major>-chat rule
        [InlineData("omni-moderation-latest")]
        [InlineData("o")]
        [InlineData("gpt-")]
        [InlineData("gpt-99999999999999999999")]
        public void RegularModels_SendSamplingAndNoEffort(string model)
        {
            var probe = new OpenAIProbe(model);
            foreach (var effort in AllEfforts)
                AssertBody(probe, effort, expectedEffort: null, expectSampling: true);
        }

        [Theory]
        [InlineData("gpt-5")]
        [InlineData("gpt-5-mini")]
        [InlineData("GPT-5-Nano")]
        [InlineData("gpt-5-codex")]             // Responses-only: rule test
        [InlineData("gpt-5-pro")]               // Responses-only: rule test
        public void Gpt5_0_SendsMinimalForNone(string model)
        {
            var probe = new OpenAIProbe(model);
            AssertBody(probe, null, null, expectSampling: false);
            AssertBody(probe, ReasoningEffort.None, "minimal", expectSampling: false);
            AssertBody(probe, ReasoningEffort.Low, "low", expectSampling: false);
            AssertBody(probe, ReasoningEffort.Medium, "medium", expectSampling: false);
            AssertBody(probe, ReasoningEffort.High, "high", expectSampling: false);
        }

        [Theory]
        [InlineData("gpt-5.1")]
        [InlineData("gpt-5.2")]
        [InlineData("gpt-5.4-mini")]
        [InlineData("gpt-5.5")]
        [InlineData("gpt-5.6")]
        [InlineData("gpt-5.2-codex")]           // Responses-only: rule test
        [InlineData("gpt-6-luna")]
        [InlineData("gpt-6-sol")]
        [InlineData("gpt-6.1-terra")]
        [InlineData("gpt-6-cyber")]
        [InlineData("gpt-10")]                  // hypothetical future generation
        [InlineData("gpt-5.1-chat-latest")]     // 3.2.1 rule kept
        public void CanTurnOffModels_SendNoneWithSampling(string model)
        {
            var probe = new OpenAIProbe(model);
            AssertBody(probe, null, null, expectSampling: false);
            AssertBody(probe, ReasoningEffort.None, "none", expectSampling: true);
            AssertBody(probe, ReasoningEffort.Low, "low", expectSampling: false);
            AssertBody(probe, ReasoningEffort.Medium, "medium", expectSampling: false);
            AssertBody(probe, ReasoningEffort.High, "high", expectSampling: false);
        }

        [Theory]
        [InlineData("o1")]
        [InlineData("o1-mini")]
        [InlineData("o3")]
        [InlineData("o3-pro")]                  // Responses-only: rule test
        [InlineData("o4-mini")]
        [InlineData("o5")]                      // hypothetical future o-series
        [InlineData("gpt-6-astra")]
        [InlineData("gpt-6.1-sol")]
        public void AlwaysOnModels_SendLowForNone(string model)
        {
            var probe = new OpenAIProbe(model);
            AssertBody(probe, null, null, expectSampling: false);
            AssertBody(probe, ReasoningEffort.None, "low", expectSampling: false);
            AssertBody(probe, ReasoningEffort.Low, "low", expectSampling: false);
            AssertBody(probe, ReasoningEffort.Medium, "medium", expectSampling: false);
            AssertBody(probe, ReasoningEffort.High, "high", expectSampling: false);
        }

        [Fact]
        public void Override_UnknownReasoningModel_IsTreatedAsCanTurnOff()
        {
            var probe = new OpenAIProbe("my-deployment", isReasoningModel: true);
            AssertBody(probe, ReasoningEffort.High, "high", expectSampling: false);
            AssertBody(probe, ReasoningEffort.None, "none", expectSampling: true);
        }

        [Fact]
        public void ChatLatest_FixedEffort_SendsNoEffortAndNoSampling()
        {
            // The live API accepts only reasoning_effort "medium" and the default temperature for chat-latest
            var probe = new OpenAIProbe("chat-latest");
            foreach (var effort in AllEfforts)
                AssertBody(probe, effort, expectedEffort: null, expectSampling: false);

            var caps = probe.GetCapabilities(RequestJson.Hi(ReasoningEffort.High));
            Assert.False(caps.SupportsReasoningEffort);
            Assert.False(caps.CanTurnOffReasoning);
            Assert.False(caps.SupportsTemperature);
        }

        [Fact]
        public void Override_KnownReasoningModel_KeepsItsCategory()
        {
            AssertBody(new OpenAIProbe("o3", isReasoningModel: true), ReasoningEffort.None, "low", expectSampling: false);
        }

        [Fact]
        public void Override_NotAReasoningModel_IsRegular()
        {
            AssertBody(new OpenAIProbe("gpt-5", isReasoningModel: false), ReasoningEffort.High, null, expectSampling: true);
        }

        [Fact]
        public void Timeout_IsThreeMinutes()
        {
            Assert.Equal(TimeSpan.FromMinutes(3), new OpenAIProbe("gpt-5").Timeout);
        }
    }
}
