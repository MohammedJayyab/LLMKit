# LLMKit

[![NuGet](https://img.shields.io/nuget/v/LLMKit.svg)](https://www.nuget.org/packages/LLMKit)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

LLMKit is a thread-safe .NET library that provides a unified interface for interacting with various Large Language Models (LLMs) including OpenAI, Gemini, and DeepSeek.

## Features

- Unified interface for multiple LLM providers
- Thread-safe implementation
- Conversation management
- Fluent API for message building
- Configurable parameters (tokens, temperature, etc.)
- Comprehensive error handling
- Dependency injection support
- Cancellation token support
- Custom endpoint support for all providers

## Installation

```powershell
Install-Package LLMKit
```
## Requirements

- .NET 8.0 or later
- Valid API keys for the LLM providers

## Quick Start

```csharp
using LLMKit;
using LLMKit.Providers;

var client = new LLMClient(
    new OpenAIProvider(apiKey: "your-api-key", model: "gpt-3.5-turbo")
);

string response = await client.GenerateTextAsync(
    "You are a helpful assistant.",
    "What is the capital of France?"
);
```

## Usage Examples

### Basic Conversation
```csharp
var client = new LLMClient(new OpenAIProvider("your-api-key", "gpt-3.5-turbo"));
var conversation = client.StartConversation();

await client.SendMessageAsync("Hello, how are you?");
await client.SendMessageAsync("What's the weather like?");
await client.SendMessageAsync("Tell me a joke");

string history = client.GetFormattedConversation();
```

### Custom Parameters
```csharp
var client = new LLMClient(
    new OpenAIProvider("your-api-key", "gpt-3.5-turbo"),
    defaultMaxTokens: 1000,
    defaultTemperature: 0.7
);
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
    var response = await client.GenerateTextAsync(
        "You are a helpful assistant.",
        "What is the capital of France?"
    );
}
catch (LLMException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Cancellation
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var response = await client.GenerateTextAsync(
    "You are a helpful assistant.",
    "What is the capital of France?",
    cts.Token
);
```

## Supported Providers

### OpenAI
```csharp
var provider = new OpenAIProvider(
    apiKey: "your-api-key",
    model: "gpt-3.5-turbo"
);
```

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

### Custom Endpoints
Each provider supports custom endpoints. If not provided, the library will use the default endpoint for that provider.

```csharp
// OpenAI with custom endpoint
var client = new LLMClient(new OpenAIProvider(
    apiKey: "your-api-key",
    model: "gpt-3.5-turbo",
    endpoint: new Uri("https://api.openai.com/v1/chat/completions")
));

// Gemini with custom endpoint
var client = new LLMClient(new GeminiProvider(
    apiKey: "your-api-key",
    model: "gemini-pro",
    endpoint: new Uri("https://generativelanguage.googleapis.com/v1beta/models")
));

// DeepSeek with custom endpoint
var client = new LLMClient(new DeepSeekProvider(
    apiKey: "your-api-key",
    model: "deepseek-chat",
    endpoint: new Uri("https://api.deepseek.com/v1/chat/completions")
));
```


## License

MIT License. See [LICENSE](LICENSE) for details.

## Support

For issues or questions, please open an issue in the GitHub repository.