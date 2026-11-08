using System.Net;
using System.Text;
using LLMKit.Providers;

namespace LLMKit.Tests
{
    /// <summary>
    /// Answers every request with a fixed JSON body and records the last request body; no network is used.
    /// </summary>
    internal sealed class StubHttpHandler : HttpMessageHandler
    {
        public string ResponseJson { get; set; } = "{}";

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(ResponseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    // Response probes return a canned provider response. The handler field is initialized
    // before the base constructor calls CreateHttpClient.

    internal sealed class OpenAIResponseProbe : OpenAIProvider
    {
        private readonly StubHttpHandler _handler = new();
        public OpenAIResponseProbe(string responseJson) : base("test-key", "gpt-6-luna") => _handler.ResponseJson = responseJson;
        public string? LastRequestBody => _handler.LastRequestBody;

        // Answers the next calls with this body
        public void RespondWith(string responseJson) => _handler.ResponseJson = responseJson;

        // Makes the next calls fail with this status and body (400 is not retried)
        public void FailWith(HttpStatusCode statusCode, string errorJson)
        {
            _handler.StatusCode = statusCode;
            _handler.ResponseJson = errorJson;
        }

        protected override HttpClient CreateHttpClient() => new(_handler);
    }

    internal sealed class AnthropicResponseProbe : AnthropicProvider
    {
        private readonly StubHttpHandler _handler = new();
        public AnthropicResponseProbe(string responseJson) : base("test-key", "claude-opus-5-5") => _handler.ResponseJson = responseJson;
        protected override HttpClient CreateHttpClient() => new(_handler);
    }

    internal sealed class GeminiResponseProbe : GeminiProvider
    {
        private readonly StubHttpHandler _handler = new();
        public GeminiResponseProbe(string responseJson) : base("test-key", "gemini-3.6-flash") => _handler.ResponseJson = responseJson;
        protected override HttpClient CreateHttpClient() => new(_handler);
    }

    internal sealed class DeepSeekResponseProbe : DeepSeekProvider
    {
        private readonly StubHttpHandler _handler = new();
        public DeepSeekResponseProbe(string responseJson) : base("test-key", "deepseek-flash") => _handler.ResponseJson = responseJson;
        protected override HttpClient CreateHttpClient() => new(_handler);
    }

    internal sealed class LLMStudioResponseProbe : LLMStudioProvider
    {
        private readonly StubHttpHandler _handler = new();
        public LLMStudioResponseProbe(string responseJson) : base(null, "glm-4.6v-flash") => _handler.ResponseJson = responseJson;
        protected override HttpClient CreateHttpClient() => new(_handler);
    }
}
