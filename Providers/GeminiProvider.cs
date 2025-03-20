// Providers/GeminiProvider.cs
using LLMKit.Models;
using System.Text.Json;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with Google's Gemini LLM service.
    /// </summary>
    public class GeminiProvider : BaseLLMProvider
    {
        protected override string BaseEndpoint => Constants.GeminiEndpoint;

        public GeminiProvider(string apiKey, string model) : base(apiKey, model)
        {
        }

        protected override object CreateRequestData(LLMRequest request)
        {
            var processedMessages = ProcessMessages(request.Messages.ToList());
            return new
            {
                contents = processedMessages.ConvertAll(m => new { parts = new[] { new { text = m.Content } } })
            };
        }

        public override async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestData = CreateRequestData(request);
                var endpoint = GetEndpointUrl();
                var responseJson = await SendRequestAsync(endpoint, requestData, cancellationToken);
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

        private string GetEndpointUrl() => $"{BaseEndpoint}/{Model}:generateContent?key={ApiKey}";

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

        private static List<ChatMessage> ProcessMessages(List<ChatMessage> messages)
        {
            var systemMessage = messages.FirstOrDefault(m => m.Role == ChatMessage.Roles.System);
            if (systemMessage == null)
            {
                return messages.ToList();
            }

            return CombineSystemMessageWithUser(messages, systemMessage);
        }

        private static List<ChatMessage> CombineSystemMessageWithUser(List<ChatMessage> messages, ChatMessage systemMessage)
        {
            var processedMessages = new List<ChatMessage>();
            var firstUserMessage = messages.FirstOrDefault(m => m.Role == ChatMessage.Roles.User);

            if (firstUserMessage != null)
            {
                processedMessages.Add(new ChatMessage
                {
                    Role = ChatMessage.Roles.User,
                    Content = CombineMessages(systemMessage.Content, firstUserMessage.Content)
                });

                processedMessages.AddRange(
                    messages.Where(m => m.Role != ChatMessage.Roles.System && m != firstUserMessage)
                );
            }
            else
            {
                processedMessages.Add(new ChatMessage
                {
                    Role = ChatMessage.Roles.User,
                    Content = systemMessage.Content
                });
            }

            return processedMessages;
        }

        private static string CombineMessages(string systemContent, string userContent)
            => $"{systemContent}\n{userContent}";
    }
}