// Providers/GeminiProvider.cs
using LLMKit.Models;
using System.Text.Json;
using System.Text;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with Google's Gemini LLM service.
    /// </summary>
    public class GeminiProvider : BaseLLMProvider
    {
        private static readonly Uri BaseEndpoint = new("https://generativelanguage.googleapis.com/v1beta/models");
       

        public GeminiProvider(string apiKey, string model, Uri? endpoint = null)
            : base(apiKey, model, ConstructEndpoint(endpoint, model, apiKey))
        {
            
        }

        private static Uri ConstructEndpoint(Uri? customEndpoint, string model, string apiKey)
        {
            if (customEndpoint != null)
            {
                var uriBuilder = new UriBuilder(customEndpoint);
                var customQuery = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                customQuery["key"] = apiKey;
                uriBuilder.Query = customQuery.ToString();
                return uriBuilder.Uri;
            }

            var builder = new UriBuilder(BaseEndpoint);
            builder.Path = $"{builder.Path}/{model}:generateContent";
            var defaultQuery = System.Web.HttpUtility.ParseQueryString(builder.Query);
            defaultQuery["key"] = apiKey;
            builder.Query = defaultQuery.ToString();
            return builder.Uri;
        }

        protected override void ConfigureHttpClient()
        {
            // No need to add API key here as it's already in the URI
        }

        protected override object CreateRequestData(LLMRequest request)
        {
            return new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = request.Messages.Last().Content }
                        }
                    }
                }
            };
        }

        public override async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestData = CreateRequestData(request);
                var responseJson = await SendRequestAsync(requestData, cancellationToken);
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
            var responseObject = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

            if (!IsValidResponse(responseObject))
            {
                return new LLMResponse { Text = string.Empty };
            }

            return new LLMResponse
            {
                Text = responseObject!.candidates[0].content.parts[0].text?.Trim() ?? string.Empty
            };
        }

        private static bool IsValidResponse(GeminiResponse? response)
        {
            return response?.candidates != null
                && response.candidates.Length > 0
                && response.candidates[0]?.content?.parts != null
                && response.candidates[0].content.parts.Length > 0;
        }

        private static string FormatMessagesForGemini(IEnumerable<ChatMessage> messages)
        {
            var conversation = new StringBuilder();
            var messagesList = messages.ToList();

            // If it's a single request (2 messages or less - system + user), include system message
            if (messagesList.Count <= 2)
            {
                foreach (var message in messagesList)
                {
                    conversation.AppendLine(message.Content);
                }
                return conversation.ToString().Trim();
            }

            // For conversations, skip system messages
            messagesList = messagesList.Where(m => m.Role != ChatMessage.Roles.System).ToList();

            for (int i = 0; i < messagesList.Count; i++)
            {
                var message = messagesList[i];
                conversation.AppendLine(message.Content);

                // Add a separator between messages
                if (i < messagesList.Count - 1)
                {
                    conversation.AppendLine();
                }
            }

            return conversation.ToString().Trim();
        }
    }
}