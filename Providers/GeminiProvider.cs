// Providers/GeminiProvider.cs
using LLMKit.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace LLMKit.Providers
{
    /// <summary>
    /// Provides integration with Google's Gemini LLM service.
    /// </summary>
    public class GeminiProvider : BaseLLMProvider
    {
        private static readonly Uri BaseEndpoint = new("https://generativelanguage.googleapis.com/v1beta/models");
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

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
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            // Increase timeout for larger images
            HttpClient.Timeout = TimeSpan.FromMinutes(2);
            
            // Enable TLS 1.2 and 1.3 which might be required by Google APIs
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        }

        protected override object CreateRequestData(LLMRequest request)
        {
            // For text-only requests without multimodal content, use simpler format
            if (!request.Messages.Any(m => m.IsMultimodal))
            {
                return CreateTextOnlyRequestData(request);
            }
            
            // Handle multimodal content
            return CreateMultimodalRequestData(request);
        }
        
        private object CreateTextOnlyRequestData(LLMRequest request)
        {
            var contents = new List<Dictionary<string, object>>();
            string? systemMessage = null;
            
            foreach (var message in request.Messages)
            {
                if (message.Role == ChatMessage.Roles.System)
                {
                    systemMessage = message.Content;
                    continue;
                }
                
                contents.Add(new Dictionary<string, object>
                {
                    { "role", message.Role == ChatMessage.Roles.User ? "user" : "model" },
                    { "parts", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "text", message.Content } }
                        }
                    }
                });
            }
            
            var requestObject = new Dictionary<string, object>
            {
                { "contents", contents },
                { "generation_config", new Dictionary<string, object>
                    {
                        { "temperature", request.Temperature },
                        { "top_p", request.TopP },
                        { "max_output_tokens", request.MaxTokens }
                    }
                }
            };
            
            if (!string.IsNullOrEmpty(systemMessage))
            {
                requestObject["system_instruction"] = new Dictionary<string, object>
                {
                    { "parts", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "text", systemMessage } }
                        }
                    }
                };
            }
            
            return requestObject;
        }
        
        private object CreateMultimodalRequestData(LLMRequest request)
        {
            var contents = new List<Dictionary<string, object>>();
            string? systemMessage = null;
            
            foreach (var message in request.Messages)
            {
                if (message.Role == ChatMessage.Roles.System)
                {
                    systemMessage = message.Content;
                    continue;
                }
                
                var parts = new List<Dictionary<string, object>>();
                
                if (message.IsMultimodal)
                {
                    foreach (var item in message.ContentItems)
                    {
                        if (item.Type == MessageContent.ContentType.Text && !string.IsNullOrEmpty(item.Text))
                        {
                            parts.Add(new Dictionary<string, object> { { "text", item.Text } });
                        }
                        else if (item.Type == MessageContent.ContentType.Image && !string.IsNullOrEmpty(item.ImageUrl))
                        {
                            AddImagePart(parts, item);
                        }
                    }
                }
                else
                {
                    parts.Add(new Dictionary<string, object> { { "text", message.Content } });
                }
                
                contents.Add(new Dictionary<string, object>
                {
                    { "role", message.Role == ChatMessage.Roles.User ? "user" : "model" },
                    { "parts", parts }
                });
            }
            
            var requestObject = new Dictionary<string, object>
            {
                { "contents", contents },
                { "generation_config", new Dictionary<string, object>
                    {
                        { "temperature", request.Temperature },
                        { "top_p", request.TopP },
                        { "max_output_tokens", request.MaxTokens }
                    }
                }
            };
            
            if (!string.IsNullOrEmpty(systemMessage))
            {
                requestObject["system_instruction"] = new Dictionary<string, object>
                {
                    { "parts", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "text", systemMessage } }
                        }
                    }
                };
            }
            
            return requestObject;
        }
        
        private void AddImagePart(List<Dictionary<string, object>> parts, MessageContent item)
        {
            string url = item.ImageUrl!;
            
            // Data URI format
            if (url.StartsWith("data:"))
            {
                parts.Add(new Dictionary<string, object>
                {
                    { "inline_data", new Dictionary<string, object>
                        {
                            { "mime_type", item.ImageMimeType ?? GetMimeTypeFromDataUri(url) },
                            { "data", ExtractBase64FromDataUri(url) }
                        }
                    }
                });
                return;
            }
            
            // Web URL
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
                (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                parts.Add(new Dictionary<string, object>
                {
                    { "file_data", new Dictionary<string, object>
                        {
                            { "mime_type", item.ImageMimeType ?? "image/jpeg" },
                            { "file_uri", url }
                        }
                    }
                });
                return;
            }
            
            // Local file path
            if (File.Exists(url))
            {
                try
                {
                    var mimeType = item.ImageMimeType ?? GetMimeTypeFromExtension(Path.GetExtension(url));
                    var imageBytes = File.ReadAllBytes(url);
                    var imageBase64 = Convert.ToBase64String(imageBytes);
                    
                    parts.Add(new Dictionary<string, object>
                    {
                        { "inline_data", new Dictionary<string, object>
                            {
                                { "mime_type", mimeType },
                                { "data", imageBase64 }
                            }
                        }
                    });
                    return;
                }
                catch
                {
                    return; // Skip this image if it can't be loaded
                }
            }
            
            // Raw base64 data
            string rawBase64 = url;
            if (rawBase64.Contains("base64,"))
            {
                rawBase64 = ExtractBase64FromDataUri(rawBase64);
            }
            
            parts.Add(new Dictionary<string, object>
            {
                { "inline_data", new Dictionary<string, object>
                    {
                        { "mime_type", item.ImageMimeType ?? "image/jpeg" },
                        { "data", rawBase64 }
                    }
                }
            });
        }
        
        private string GetMimeTypeFromDataUri(string dataUri)
        {
            int startIndex = dataUri.IndexOf(':') + 1;
            int endIndex = dataUri.IndexOf(';');
            if (startIndex > 0 && endIndex > startIndex)
            {
                return dataUri.Substring(startIndex, endIndex - startIndex);
            }
            return "image/jpeg"; // Default
        }
        
        private string ExtractBase64FromDataUri(string dataUri)
        {
            int base64Index = dataUri.IndexOf("base64,");
            if (base64Index >= 0)
            {
                return dataUri.Substring(base64Index + 7);
            }
            return dataUri; // Return original if not found
        }
        
        private string GetMimeTypeFromExtension(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg" // Default to JPEG
            };
        }

        public override async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestData = CreateRequestData(request);
                var json = JsonSerializer.Serialize(requestData, JsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await HttpClient.PostAsync(Endpoint, content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Error response from Gemini API: {response.StatusCode} - {errorContent}");
                }
                
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseResponse(responseJson);
            }
            catch (HttpRequestException ex)
            {
                throw CreateProviderException($"Network error occurred: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw CreateProviderException($"Error processing API response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw CreateProviderException($"An unexpected error occurred: {ex.Message}", ex);
            }
        }

        private static LLMResponse ParseResponse(string responseJson)
        {
            try
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
            catch (Exception ex)
            {
                throw new JsonException($"Error parsing Gemini response: {ex.Message}", ex);
            }
        }

        private static bool IsValidResponse(GeminiResponse? response)
        {
            return response?.candidates != null
                && response.candidates.Length > 0
                && response.candidates[0]?.content?.parts != null
                && response.candidates[0].content.parts.Length > 0;
        }
    }
}