# API Reference

This document provides detailed API documentation for LLMKit.

## LLMClient

The main client class for interacting with LLM providers.

### Constructor
```csharp
public LLMClient(
    ILLMProvider provider,
    IConversationService conversationService,
    int? defaultMaxTokens = null,
    double? defaultTemperature = null)
```

### Properties
```csharp
public Conversation? CurrentConversation { get; }
```

### Methods

#### StartConversation
```csharp
public Conversation StartConversation(int maxMessages = Constants.DefaultMaxMessages)
```
Starts a new conversation with the specified maximum number of messages.

#### GenerateTextAsync
```csharp
public async Task<string> GenerateTextAsync(
    string systemMessage,
    string userMessage,
    CancellationToken cancellationToken = default)
```
Generates text using the specified system and user messages.

#### SendMessageAsync
```csharp
public async Task<string> SendMessageAsync(
    string message,
    CancellationToken cancellationToken = default)
```
Sends a message in the current conversation and gets a response.

#### ClearConversation
```csharp
public void ClearConversation()
```
Clears the current conversation.

#### GetFormattedConversation
```csharp
public string? GetFormattedConversation()
```
Gets the formatted conversation history.

#### Dispose
```csharp
public void Dispose()
```
Releases all resources used by the LLMClient.

## ILLMProvider

Interface for LLM providers.

### Methods

#### GenerateTextAsync
```csharp
Task<LLMResponse> GenerateTextAsync(
    LLMRequest request,
    CancellationToken cancellationToken = default)
```

## IConversationService

Interface for managing conversations.

### Methods

#### CreateConversation
```csharp
Conversation CreateConversation(int maxMessages)
```

#### AddMessage
```csharp
void AddMessage(Conversation conversation, string role, string content)
```

#### ClearConversation
```csharp
void ClearConversation(Conversation conversation)
```

## ChatMessageBuilder

Fluent builder for constructing chat messages.

### Methods

#### Create
```csharp
public static ChatMessageBuilder Create()
```

#### AddSystemMessage
```csharp
public ChatMessageBuilder AddSystemMessage(string content)
```

#### AddUserMessage
```csharp
public ChatMessageBuilder AddUserMessage(string content)
```

#### AddAssistantMessage
```csharp
public ChatMessageBuilder AddAssistantMessage(string content)
```

#### Build
```csharp
public IReadOnlyList<ChatMessage> Build()
```

## Models

### LLMRequest
```csharp
public class LLMRequest
{
    public IEnumerable<ChatMessage> Messages { get; private set; }
    public int? MaxTokens { get; private set; }
    public double? Temperature { get; private set; }

    public LLMRequest WithMessages(IEnumerable<ChatMessage> messages)
    public LLMRequest WithMaxTokens(int? maxTokens)
    public LLMRequest WithTemperature(double? temperature)
}
```

### LLMResponse
```csharp
public class LLMResponse
{
    public string Text { get; }
    public int TokensUsed { get; }
    public TimeSpan ProcessingTime { get; }
}
```

### ChatMessage
```csharp
public class ChatMessage
{
    public string Role { get; }
    public string Content { get; }
    public DateTime Timestamp { get; }

    public static class Roles
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";
    }
}
```

### Conversation
```csharp
public class Conversation
{
    public IReadOnlyList<ChatMessage> Messages { get; }
    public int MessageCount { get; }
    public int MaxMessages { get; }

    public void AddMessage(string role, string content)
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
    public const int DefaultMaxMessages = 10;
    public const double DefaultTemperature = 0.7;
    public const int DefaultMaxTokens = 2000;
}
``` 