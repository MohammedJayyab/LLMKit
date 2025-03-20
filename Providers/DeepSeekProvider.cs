// Providers/DeepSeekProvider.cs
using LLMKit.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with DeepSeek's LLM service.
    /// </summary>
    public class DeepSeekProvider : BaseLLMProvider
    {
        protected override string BaseEndpoint => Constants.DeepSeekEndpoint;

        public DeepSeekProvider(string apiKey, string model) : base(apiKey, model)
        {
        }

        protected override void ConfigureHttpClient()
        {
            base.ConfigureHttpClient();
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(Constants.BearerAuth, ApiKey);
        }

        public override async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestData = CreateRequestData(request);
                var responseJson = await SendRequestAsync(BaseEndpoint, requestData, cancellationToken);

                return ParseResponse(responseJson);
            }
            catch (HttpRequestException ex)
            {
                throw CreateProviderException("Network error occurred.", ex);
            }
            catch (JsonException ex)
            {
                throw CreateProviderException("Error processing API response.", ex);
            }
            catch (Exception ex)
            {
                throw CreateProviderException("An unexpected error occurred.", ex);
            }
        }

        private static LLMResponse ParseResponse(string responseJson)
        {
            var responseObject = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson);

            if (!IsValidResponse(responseObject))
            {
                return new LLMResponse { Text = string.Empty };
            }

            return new LLMResponse
            {
                Text = responseObject!.choices[0].message.content?.Trim() ?? string.Empty
            };
        }

        private static bool IsValidResponse(DeepSeekResponse? response)
        {
            return response?.choices != null
                && response.choices.Length > 0
                && response.choices[0]?.message?.content != null
                && !string.IsNullOrWhiteSpace(response.choices[0].message.content);
        }
    }
}