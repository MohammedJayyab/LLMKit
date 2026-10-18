# LLMKit

[![NuGet](https://img.shields.io/nuget/v/LLMKit.svg)](https://www.nuget.org/packages/LLMKit)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-blue.svg)](https://github.com/MohammedJayyab/LLMKit)
[![Documentation](https://img.shields.io/badge/docs-wiki-blue.svg)](https://github.com/MohammedJayyab/LLMKit/wiki)
[![Downloads](https://img.shields.io/nuget/dt/LLMKit.svg)](https://www.nuget.org/packages/LLMKit)

LLMKit is a thread-safe .NET library that provides a unified interface for interacting with various Large Language Models (LLMs) including OpenAI, Anthropic (Claude), Gemini, DeepSeek, and LLM Studio (local models).

## Features

- Unified interface for multiple LLM providers
- Thread-safe implementation
- Built-in conversation management (stores up to 15 messages by default)
- ✨ **NEW!** Multimodal support (text + images)
- ✨ **NEW!** Reasoning / thinking effort control and access to the returned reasoning
- ✨ **NEW!** Anthropic (Claude) provider
- Fluent API for message building
- Configurable parameters (tokens, temperature, etc.)
- Comprehensive error handling with automatic retries
- Dependency injection support
- Cancellation token support
- Custom endpoint support for all providers

## ☕ Donate

If you find this project helpful, consider buying me a coffee to support its development:

[![Buy Me A Coffee](https://img.shields.io/badge/Buy_Me_A_Coffee-FFDD00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://www.buymeacoffee.com/mjayyab)

## Installation

### Package Manager
```powershell
Install-Package LLMKit
```

### .NET CLI
```bash
dotnet add package LLMKit
```

### Clone Repository
```bash
git clone https://github.com/MohammedJayyab/LLMKit.git
```

## Requirements

- .NET 8.0 or later
- Valid API keys for the LLM providers

## Quick Start

```csharp
using LLMKit;
using LLMKit.Providers;

// Using statement ensures proper disposal
using var client = new LLMClient(
    new OpenAIProvider(apiKey: "your-api-key", model: "gpt-3.5-turbo")
);

// Conversation history is automatically managed
string response = await client.GenerateTextAsync("What is the capital of France?");
Console.WriteLine(response);
```

## Usage Examples

### Conversation Management
```csharp
using var client = new LLMClient(new OpenAIProvider("your-api-key", "gpt-3.5-turbo"));

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

### ✨ Multimodal Support (Image + Text)
```csharp
// Generate a response to a message with an image
string response = await client.GenerateTextWithImageAsync(
    "What can you see in this image?", 
    "path/to/your/image.jpg"
);
```

### Custom Parameters
```csharp
// Create client with custom parameters
var client = new LLMClient(
    provider: new OpenAIProvider("your-api-key", "gpt-3.5-turbo"),
    maxTokens: 1000,
    temperature: 0.7,
    maxMessages: 20  // Store up to 20 messages in conversation history
);
```

### ✨ Setting a System Message
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

### Error Handling
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

### Cancellation
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
string response = await client.GenerateTextAsync(
    "What is the capital of France?",
    cts.Token
);
```

## Reasoning / Thinking

Set how much a model should reason before answering. The same setting works for every provider
and is mapped to the provider's own option. Leave it unset to use the model's default.

```csharp
using LLMKit.Models; // ReasoningEffort

using var client = new LLMClient(
    new GeminiProvider("your-api-key", "gemini-2.5-flash"),
    reasoningEffort: ReasoningEffort.High
);

string answer = await client.GenerateTextAsync("Explain the Monty Hall problem.");
string? reasoning = client.LastReasoning; // null if the provider returned none
```

When calling a provider directly, use `new LLMRequest().WithReasoningEffort(...)` and read
`LLMResponse.Reasoning`.

| Provider | Effort is sent as | Reasoning returned |
|---|---|---|
| OpenAI | `reasoning_effort` (reasoning models only) | No (Chat Completions does not return it) |
| Anthropic | adaptive `thinking` plus `output_config.effort` | Yes, thought summaries |
| Gemini | `thinking_budget` for 2.5 models, `thinking_level` for newer ones | Yes, thought summaries |
| DeepSeek | `thinking` on/off plus `reasoning_effort` | Yes, `reasoning_content` |
| LLM Studio | `reasoning_effort` | Yes, `reasoning` or `reasoning_content` |

Notes:
- Anthropic: `None` leaves thinking unset and uses `low` effort. Thinking is adaptive, which needs Claude 4.6 or later.
- `None` turns reasoning off where the model allows it. Gemini 2.5 Pro and Gemini 3 models cannot
  fully disable it, so `None` uses the lowest setting there.
- Gemini 2.5 budgets: Low = 1024, Medium = 8192, High = 24576 tokens.
- DeepSeek maps medium to high itself, and ignores sampling parameters while thinking.
- LLM Studio's `reasoning_effort` support depends on the server version (0.4.8 or later) and model.

## Supported Providers

### OpenAI
```csharp
var provider = new OpenAIProvider(
    apiKey: "your-api-key",
    model: "gpt-3.5-turbo"
);
```

**Reasoning models (GPT-5, o-series):** these models reject `temperature`, `top_p` and the penalty
parameters, so LLMKit omits them automatically (unless the reasoning effort is `None`, GPT-5.1 and
later). Reasoning tokens count against `maxTokens`, so use a generous value or the response text
may come back empty. See [Reasoning / Thinking](#reasoning--thinking).

### Anthropic (Claude)
```csharp
var provider = new AnthropicProvider(
    apiKey: "your-api-key",
    model: "claude-opus-5-5"
);
```

Newer Claude models (Opus 4.7 and later, Sonnet 5, Fable, Haiku 5) reject `temperature`, so LLMKit
omits it for them. Sampling parameters other than `temperature` are never sent to Anthropic.

### Gemini
```csharp
var provider = new GeminiProvider(
    apiKey: "your-api-key",
    model: "gemini-2.0-flash"
);
```

### DeepSeek
```csharp
var provider = new DeepSeekProvider(
    apiKey: "your-api-key",
    model: "deepseek-chat"
);
```

### LLM Studio (Local Models)
```csharp
// For local LLM Studio servers (API key is optional)
var provider = new LLMStudioProvider(
    apiKey: null,  // Can be null for local servers without authentication
    model: "glm-4.6v-flash"
);

// Or with a custom endpoint
var provider = new LLMStudioProvider(
    apiKey: null,
    model: "glm-4.6v-flash",
    endpoint: new Uri("http://localhost:1234/v1/chat/completions")
);
```

### Custom Endpoints
Each provider supports custom endpoints. If not provided, the library will use the default endpoint for that provider.

```csharp
// OpenAI with custom endpoint
var client = new LLMClient(new OpenAIProvider(
    apiKey: "your-api-key",
    model: "gpt-3.5-turbo",
    endpoint: new Uri("https://api.openai.com/v1/chat/completions")
));

// Anthropic with custom endpoint
var client = new LLMClient(new AnthropicProvider(
    apiKey: "your-api-key",
    model: "claude-opus-5-5",
    endpoint: new Uri("https://api.anthropic.com/v1/messages")
));

// Gemini with custom endpoint
var client = new LLMClient(new GeminiProvider(
    apiKey: "your-api-key",
    model: "gemini-2.0-flash",
    endpoint: new Uri("https://generativelanguage.googleapis.com/v1beta/models")
));

// DeepSeek with custom endpoint
var client = new LLMClient(new DeepSeekProvider(
    apiKey: "your-api-key",
    model: "deepseek-chat",
    endpoint: new Uri("https://api.deepseek.com/v1/chat/completions")
));

// LLM Studio with custom endpoint
var client = new LLMClient(new LLMStudioProvider(
    apiKey: null,
    model: "glm-4.6v-flash",
    endpoint: new Uri("http://localhost:1234/v1/chat/completions")
));
```

## Documentation 📚

For more detailed information, please visit our [Wiki](https://github.com/MohammedJayyab/LLMKit/wiki):

- [Getting Started Guide](https://github.com/MohammedJayyab/LLMKit/wiki/Getting-Started)
- [Architecture Overview](https://github.com/MohammedJayyab/LLMKit/wiki/Architecture)
- [API Reference](https://github.com/MohammedJayyab/LLMKit/wiki/API-Reference)
- [Examples](https://github.com/MohammedJayyab/LLMKit/wiki/Examples)
- [Contributing Guidelines](https://github.com/MohammedJayyab/LLMKit/wiki/Contributing)
- [Roadmap](https://github.com/MohammedJayyab/LLMKit/wiki/Roadmap)

## License

MIT License. See [LICENSE](LICENSE) for details.

## Support

For issues or questions, please open an issue in the GitHub repository.

