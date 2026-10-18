# Getting Started with LLMKit

This guide will help you get started with LLMKit in your .NET projects.

## Installation

### NuGet Package Manager
```powershell
Install-Package LLMKit
```

### .NET CLI
```bash
dotnet add package LLMKit
```

## Basic Usage

### Simple Text Generation
```csharp
using LLMKit;
using LLMKit.Providers;

// Using statement ensures proper disposal
using var client = new LLMClient(
    new OpenAIProvider(apiKey: "your-api-key", model: "gpt-3.5-turbo")
);

string response = await client.GenerateTextAsync("What is the capital of France?");
Console.WriteLine(response);
```

### Conversation Management
```csharp
using var client = new LLMClient(
    new OpenAIProvider("your-api-key", "gpt-3.5-turbo")
);

// First message
string response1 = await client.GenerateTextAsync("Hello, how are you?");
Console.WriteLine(response1);

// Follow-up questions maintain conversation context automatically
string response2 = await client.GenerateTextAsync("What's the weather like?");
Console.WriteLine(response2);

// Get the formatted conversation history
string history = client.GetFormattedConversation();
Console.WriteLine(history);

// Clear the conversation if needed
client.ClearConversation();
```

### Multimodal Support (Image + Text)
```csharp
// Generate a response to a message with an image
string response = await client.GenerateTextWithImageAsync(
    "What can you see in this image?", 
    "path/to/your/image.jpg"
);
```

## Configuration

### Custom Parameters
```csharp
var client = new LLMClient(
    provider: new OpenAIProvider("your-api-key", "gpt-3.5-turbo"),
    maxTokens: 1000,
    temperature: 0.7,
    maxMessages: 20  // Store up to 20 messages in conversation history
);
```

### Setting a System Message
```csharp
// Set or update the system message
client.SetSystemMessage("You are a helpful assistant specialized in biology.");
```

### Dependency Injection
```csharp
services.AddSingleton<ILLMProvider>(sp => 
    new OpenAIProvider(
        apiKey: Configuration["OpenAI:ApiKey"],
        model: Configuration["OpenAI:Model"]
    )
);
services.AddSingleton<LLMClient>();
```

## Error Handling

```csharp
try
{
    string response = await client.GenerateTextAsync("What is the capital of France?");
    Console.WriteLine(response);
}
catch (LLMException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Cancellation Support

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
string response = await client.GenerateTextAsync(
    "What is the capital of France?",
    cts.Token
);
```

## Supported Providers

### OpenAI
```csharp
using var client = new LLMClient(
    new OpenAIProvider(apiKey: "your-api-key", model: "gpt-3.5-turbo")
);
```

### Anthropic (Claude)
```csharp
using var client = new LLMClient(
    new AnthropicProvider(apiKey: "your-api-key", model: "claude-opus-5-5")
);
```

### Gemini
```csharp
using var client = new LLMClient(
    new GeminiProvider(apiKey: "your-api-key", model: "gemini-2.0-flash")
);
```

### DeepSeek
```csharp
using var client = new LLMClient(
    new DeepSeekProvider(apiKey: "your-api-key", model: "deepseek-chat")
);
```

### LLM Studio (Local Models)
```csharp
// For local LLM Studio servers - API key is optional
using var client = new LLMClient(
    new LLMStudioProvider(
        apiKey: null,  // Can be null for local servers without authentication
        model: "glm-4.6v-flash"
    )
);

// With custom endpoint
using var client = new LLMClient(
    new LLMStudioProvider(
        apiKey: null,
        model: "glm-4.6v-flash",
        endpoint: new Uri("http://localhost:1234/v1/chat/completions")
    )
);
```

## Reasoning / Thinking

Set how much a model should reason before answering. The same setting works for every provider and
is mapped to the provider's own option. Leave it unset to use the model's default.

```csharp
using LLMKit.Models; // ReasoningEffort

using var client = new LLMClient(
    new GeminiProvider(apiKey: "your-api-key", model: "gemini-2.5-flash"),
    reasoningEffort: ReasoningEffort.High
);

string answer = await client.GenerateTextAsync("Explain the Monty Hall problem.");
string? reasoning = client.LastReasoning; // null if the provider returned none
```

| Provider | Effort is sent as | Reasoning returned |
|---|---|---|
| OpenAI | `reasoning_effort` (reasoning models only) | No (Chat Completions does not return it) |
| Anthropic | adaptive `thinking` plus `output_config.effort` | Yes, thought summaries |
| Gemini | `thinking_budget` for 2.5 models, `thinking_level` for newer ones | Yes, thought summaries |
| DeepSeek | `thinking` on/off plus `reasoning_effort` | Yes, `reasoning_content` |
| LLM Studio | `reasoning_effort` | Yes, `reasoning` or `reasoning_content` |

When calling a provider directly, use `new LLMRequest().WithReasoningEffort(...)` and read
`LLMResponse.Reasoning`.

Notes:
- Reasoning models (GPT-5, o-series) reject `temperature`, `top_p` and the penalty parameters, so
  LLMKit omits them automatically (unless the reasoning effort is `None`, GPT-5.1 and later).
  Reasoning tokens count against `maxTokens`, so use a generous value.
- Anthropic: `None` leaves thinking unset and uses `low` effort. Thinking is adaptive, which needs Claude 4.6 or later.
- `None` turns reasoning off where the model allows it. Gemini 2.5 Pro and Gemini 3 models cannot
  fully disable it, so `None` uses the lowest setting there.
- Gemini 2.5 budgets: Low = 1024, Medium = 8192, High = 24576 tokens.
- DeepSeek maps medium to high itself, and ignores sampling parameters while thinking.
- LLM Studio's `reasoning_effort` support depends on the server version (0.4.8 or later) and model.

## Next Steps

- Check out our [Examples](Examples) page for more detailed usage scenarios
- Read the [API Reference](API-Reference) for detailed documentation
- Learn about the [Architecture](Architecture) to understand how LLMKit works
- Consider [Contributing](Contributing) to the project 