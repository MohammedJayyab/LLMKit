# API Reference

This document provides detailed API documentation for LLMKit.

## LLMClient

The main client class for interacting with LLM providers.

### Constructor
```csharp
public LLMClient(
    ILLMProvider provider,
    int maxTokens = Constants.DefaultMaxTokens,
    double temperature = Constants.DefaultTemperature,
    int maxMessages = Constants.DefaultMaxMessages,
    ReasoningEffort? reasoningEffort = null)
```

`maxTokens` is the maximum number of tokens the model may generate per response, reasoning plus
answer; the input is not counted. LLMKit has no upper limit (values below 1 are raised to 1): the
provider rejects a value above the model's limit with an error.

`reasoningEffort` sets how much the model reasons before answering. Null uses the model's default.

### Properties
```csharp
public Conversation CurrentConversation { get; }
public string? LastReasoning { get; }
public TokenUsage? LastUsage { get; }
public FinishReason? LastFinishReason { get; }
public ProviderCapabilities Capabilities { get; }
```

`LastReasoning` is the reasoning returned with the last response, or null if the provider returned none.

`LastUsage` is the token counts of the last response, or null if the provider returned none. See [TokenUsage](#tokenusage).

`LastFinishReason` is why the model stopped for the last response, or null if the provider did not report it. See [FinishReason](#finishreason).

All three describe the same response and are null after a failed call.

`Capabilities` is what the provider does for its model with this client's settings, for example whether the reasoning effort and the temperature are applied. See [ProviderCapabilities](#providercapabilities).

### Methods

#### GenerateTextAsync
```csharp
public async Task<string> GenerateTextAsync(
    string userMessage,
    CancellationToken cancellationToken = default)
```
Generates text using the user message. The conversation context is automatically managed.

#### GenerateTextWithImageAsync
```csharp
public async Task<string> GenerateTextWithImageAsync(
    string userMessage,
    string imagePath,
    CancellationToken cancellationToken = default)
```
Generates a response to a message with an image. The image is automatically processed and included with the message.

#### SetSystemMessage
```csharp
public void SetSystemMessage(string? systemMessage = Constants.SystemMessage)
```
Sets or updates the system message in the conversation.

#### UpdateMaxMessages / GetMaxMessages
```csharp
public void UpdateMaxMessages(int maxMessages)
public int GetMaxMessages()
```
Changes or reads the maximum number of messages kept in the conversation.

#### GetAllSettings
```csharp
public string GetAllSettings()
```
Gets a formatted string with the current client settings.

#### GetSupportedProviders
```csharp
public static IReadOnlyList<string> GetSupportedProviders()
```
Gets the names of the providers implemented in LLMKit, sorted alphabetically (`Anthropic`, `DeepSeek`, `Gemini`, `LLMStudio`, `OpenAI`). Each name is the provider class name without the `Provider` suffix.

#### ClearConversation
```csharp
public void ClearConversation()
```
Clears the current conversation history and resets with the default system message.

#### GetFormattedConversation
```csharp
public string GetFormattedConversation()
```
Gets the formatted conversation history.

#### Dispose
```csharp
public void Dispose()
```
Releases all resources used by the LLMClient.

## ILLMProvider

Interface for LLM providers (it also implements `IDisposable`).

Built-in implementations: `OpenAIProvider`, `AnthropicProvider`, `GeminiProvider`, `DeepSeekProvider` and `LLMStudioProvider`.

### OpenAIProvider constructors
```csharp
public OpenAIProvider(string apiKey, string model, Uri? endpoint = null)
public OpenAIProvider(string apiKey, string model, Uri? endpoint, bool isReasoningModel)
```
The first constructor recognizes reasoning models from the model name (GPT-5 and later, o-series).
Use the second one for names LLMKit cannot classify, such as deployments behind an
OpenAI-compatible endpoint. `true` sends `reasoning_effort` and no sampling parameters; `false`
treats the model as a regular model.

### Properties
```csharp
Uri Endpoint { get; }
```

### Methods

#### GenerateTextAsync
```csharp
Task<LLMResponse> GenerateTextAsync(
    LLMRequest request,
    CancellationToken cancellationToken = default)
```

#### GetCapabilities
```csharp
ProviderCapabilities GetCapabilities(LLMRequest request)
```
Gets what the provider does for its model with the settings of the request (reasoning effort, max tokens; messages are not used). See [ProviderCapabilities](#providercapabilities). The interface has a default implementation that reports no capabilities, so custom providers keep compiling; `BaseLLMProvider` overrides it.

## Models

### ChatMessage
```csharp
public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public string Id { get; set; }
    public List<MessageContent> ContentItems { get; set; }
    public bool IsMultimodal { get; }

    public ChatMessage()
    public ChatMessage(string role, string content)
    public ChatMessage(string role, string textContent, IEnumerable<MessageContent>? additionalContent = null)
    public void AddImage(string imageUrl, string? mimeType = null)
    public void ClearImages()

    public static class Roles
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";
        public static readonly string[] All;
    }
}
```

### MessageContent
```csharp
public class MessageContent
{
    public ContentType Type { get; }
    public string? Text { get; }
    public string? ImageUrl { get; }
    public string? ImageMimeType { get; }

    public static MessageContent CreateText(string text)
    public static MessageContent CreateImage(string imageUrl, string? mimeType = null)

    public enum ContentType
    {
        Text,
        Image
    }
}
```

### LLMRequest
```csharp
public class LLMRequest
{
    public IReadOnlyList<ChatMessage> Messages { get; set; }
    public double Temperature { get; }
    public double TopP { get; }
    public double FrequencyPenalty { get; }
    public double PresencePenalty { get; }
    public int MaxTokens { get; }
    public bool Stream { get; }
    public ReasoningEffort? ReasoningEffort { get; }

    public LLMRequest WithMessages(IEnumerable<ChatMessage> messages)
    public LLMRequest WithTemperature(double? temperature)
    public LLMRequest WithTopP(double? topP)
    public LLMRequest WithFrequencyPenalty(double? frequencyPenalty)
    public LLMRequest WithPresencePenalty(double? presencePenalty)
    public LLMRequest WithMaxTokens(int? maxTokens)
    public LLMRequest WithStream(bool? stream)
    public LLMRequest WithReasoningEffort(ReasoningEffort? reasoningEffort)
}
```

### ReasoningEffort
```csharp
public enum ReasoningEffort
{
    None,
    Low,
    Medium,
    High
}
```

Each provider maps the value to its own setting. See [Getting Started](Getting-Started#reasoning--thinking).

### LLMResponse
```csharp
public class LLMResponse
{
    public string Text { get; set; }
    public string? Reasoning { get; set; }
    public TokenUsage? Usage { get; set; }
    public FinishReason? FinishReason { get; set; }
}
```

### FinishReason
```csharp
public enum FinishReason
{
    Completed,     // the answer is complete
    MaxTokens,     // stopped at maxTokens: cut off, or empty when the reasoning used all the tokens
    ContentFilter, // blocked or stopped by the provider's safety filter
    Other          // any other reason, for example a tool call
}
```
Why the model stopped, with the same meaning for every provider. Null when the provider did not
report it. LLMKit does not throw for `MaxTokens` or `ContentFilter`; check the value and inform the user.
`LLMClient` does not add an empty answer to the conversation, so the next call still works.

| Provider value | OpenAI, DeepSeek, LLM Studio (`finish_reason`) | Anthropic (`stop_reason`) | Gemini (`finishReason`) |
|---|---|---|---|
| `Completed` | `stop` | `end_turn`, `stop_sequence` | `STOP` |
| `MaxTokens` | `length` | `max_tokens` | `MAX_TOKENS` |
| `ContentFilter` | `content_filter` | `refusal` | `SAFETY`, `RECITATION`, `BLOCKLIST`, `PROHIBITED_CONTENT`, `SPII`, `IMAGE_SAFETY`, or a blocked prompt |
| `Other` | anything else | anything else | anything else |

### TokenUsage
```csharp
public class TokenUsage
{
    public int InputTokens { get; set; }      // system message, history and images
    public int OutputTokens { get; set; }     // generated tokens, including reasoning
    public int? ReasoningTokens { get; set; } // part of OutputTokens; null if not reported
    public int TotalTokens { get; }           // InputTokens + OutputTokens
}
```
The token counts reported by the provider, with the same meaning for every provider:

| Provider | InputTokens | OutputTokens | ReasoningTokens |
|---|---|---|---|
| OpenAI, DeepSeek, LLM Studio | `prompt_tokens` | `completion_tokens` | `completion_tokens_details.reasoning_tokens` |
| Anthropic | `input_tokens` + cache tokens | `output_tokens` | null (not reported separately) |
| Gemini | `promptTokenCount` | `candidatesTokenCount` + `thoughtsTokenCount` | `thoughtsTokenCount` |

### ProviderCapabilities
```csharp
public class ProviderCapabilities
{
    public bool SupportsReasoningEffort { get; init; } // the reasoning effort is sent to the model
    public bool CanTurnOffReasoning { get; init; }     // ReasoningEffort.None turns reasoning off
    public bool SupportsTemperature { get; init; }     // the temperature is applied with these settings
    public bool ReturnsReasoningText { get; init; }    // LLMResponse.Reasoning can be filled
    public bool SupportsImages { get; init; }          // images in messages are sent
}
```
What LLMKit does for a provider's model with given settings. LLMKit recognizes models by name, so for
a name it does not know the values are a best guess.

- `SupportsTemperature` depends on the settings: OpenAI and Anthropic reasoning models do not get it
  while they think; DeepSeek ignores it while thinking.
- `SupportsReasoningEffort` is false for models that reason at a fixed effort, such as OpenAI's `chat-latest`.
- `ReturnsReasoningText` is always false for OpenAI (Chat Completions does not return the reasoning).
  Gemma 4 returns its thoughts even without a reasoning effort.
- `SupportsImages` is false for DeepSeek (only the text is sent). Whether a model accepts images
  depends on the model.
- LLM Studio models are unknown to LLMKit: the values say what is sent, the server and model decide
  the rest.

### Conversation
```csharp
public class Conversation
{
    public IReadOnlyList<ChatMessage> Messages { get; }

    public Conversation(int maxMessages)
    public void AddMessage(string role, string content)
    public void AddMessage(ChatMessage message)
    public void Clear()
    public string GetFormattedConversation()
}
```

## Exceptions

### LLMException
```csharp
public class LLMException : Exception
{
    public LLMException(string message)
    public LLMException(string message, Exception innerException)
}
```

## Constants

```csharp
public static class Constants
{
    public const int DefaultMaxMessages = 15;
    public const double DefaultTemperature = 0.3;
    public const double DefaultTopP = 1.0;
    public const double DefaultFrequencyPenalty = 0.0;
    public const double DefaultPresencePenalty = 0.0;
    public const int DefaultMaxTokens = 4096; // used when no value is set; not an upper limit
    public const bool DefaultStream = false;
    public const string SystemMessage = "You are a helpful assistant that can answer questions and help with tasks.";
}
``` 