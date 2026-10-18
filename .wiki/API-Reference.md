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

`reasoningEffort` sets how much the model reasons before answering. Null uses the model's default.

### Properties
```csharp
public Conversation CurrentConversation { get; }
public string? LastReasoning { get; }
```

`LastReasoning` is the reasoning returned with the last response, or null if the provider returned none.

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
}
```

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
    public const int DefaultMaxTokens = 4096;
    public const bool DefaultStream = false;
    public const string SystemMessage = "You are a helpful assistant that can answer questions and help with tasks.";
}
``` 