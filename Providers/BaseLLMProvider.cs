using LLMKit.Exceptions;
using LLMKit.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides a base implementation for LLM providers with common functionality.
    /// This abstract class implements the ILLMProvider interface and provides shared
    /// functionality for HTTP communication, request handling, and error management.
    /// </summary>
    public abstract class BaseLLMProvider : ILLMProvider, IDisposable
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly HttpClient _httpClient;
        private bool _disposed;
        private const int MaxRetries = 3;

        public Uri Endpoint { get; protected set; }

        private static readonly TimeSpan[] RetryDelays = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        };

        /// <summary>
        /// Gets the API key used for authentication with the LLM service.
        /// </summary>
        protected string ApiKey => _apiKey;

        /// <summary>
        /// Gets the model identifier to use for text generation.
        /// </summary>
        protected string Model => _model;

        /// <summary>
        /// Gets the HTTP client used for API communication.
        /// </summary>
        protected HttpClient HttpClient => _httpClient;

        /// <summary>
        /// Initializes a new instance of the LLM provider.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="model">The model identifier to use.</param>
        /// <param name="endpoint">The endpoint for the LLM service.</param>
        /// <exception cref="ArgumentException">Thrown when apiKey, model, or endpoint is null or empty.</exception>
        protected BaseLLMProvider(string apiKey, string model, Uri endpoint)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
            if (string.IsNullOrEmpty(model))
                throw new ArgumentException("Model cannot be null or empty.", nameof(model));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            _apiKey = apiKey;
            _model = model;
            Endpoint = endpoint;
            _httpClient = CreateHttpClient();
            ConfigureHttpClient();
        }

        /// <summary>
        /// Creates a new HTTP client instance.
        /// Can be overridden to customize the HTTP client creation.
        /// </summary>
        protected virtual HttpClient CreateHttpClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        /// <summary>
        /// Configures the HTTP client with default headers.
        /// Can be overridden to add provider-specific configuration.
        /// </summary>
        protected virtual void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.JsonAccept));
        }

        /// <summary>
        /// Creates the request data object for the API call.
        /// Can be overridden to customize the request format for specific providers.
        /// </summary>
        protected virtual object CreateRequestData(LLMRequest request)
        {
            return new
            {
                model = _model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                top_p = request.TopP,
                frequency_penalty = request.FrequencyPenalty,
                presence_penalty = request.PresencePenalty,
                stream = request.Stream
            };
        }

        /// <summary>
        /// Sends an HTTP request to the specified endpoint with the given request data.
        /// </summary>
        protected async Task<string> SendRequestAsync(object requestData, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(requestData, options);
            using var content = new StringContent(json, Encoding.UTF8, Constants.JsonContentType);

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        await Task.Delay(RetryDelays[attempt - 1], cancellationToken);
                    }

                    var response = await _httpClient.PostAsync(Endpoint, content, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync(cancellationToken);
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    continue;
                }
                catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (attempt == MaxRetries)
                {
                    throw CreateProviderException($"Request failed after {MaxRetries} attempts", ex);
                }
            }

            throw new LLMException("Unexpected error in retry logic");
        }

        /// <summary>
        /// Generates text based on the provided request parameters.
        /// Must be implemented by concrete provider classes.
        /// </summary>
        public abstract Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a standardized exception with provider-specific context.
        /// </summary>
        protected LLMException CreateProviderException(string message, Exception innerException)
        {
            return new LLMException(
                $"Error occurred while calling the {GetType().Name.Replace("Provider", "")} API: {message}",
                innerException);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the BaseLLMProvider and optionally releases the managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the BaseLLMProvider and optionally releases the managed resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
        }

        ~BaseLLMProvider()
        {
            Dispose(false);
        }
    }
}