# LLMKit

[![NuGet](https://img.shields.io/nuget/v/LLMKit.svg)](https://www.nuget.org/packages/LLMKit)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-blue.svg)](https://github.com/MohammedJayyab/LLMKit)
[![Documentation](https://img.shields.io/badge/docs-wiki-blue.svg)](https://github.com/MohammedJayyab/LLMKit/wiki)
[![Downloads](https://img.shields.io/nuget/dt/LLMKit.svg)](https://www.nuget.org/packages/LLMKit)

LLMKit is a thread-safe .NET library that provides a unified interface for interacting with various Large Language Models (LLMs) including OpenAI, Gemini, and DeepSeek.

## Features

- Unified interface for multiple LLM providers
- Thread-safe implementation
- Built-in conversation management (stores up to 15 messages by default)
- ✨ **NEW!** Multimodal support (text + images)
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
    model: "gemini-2.0-flash",
    endpoint: new Uri("https://generativelanguage.googleapis.com/v1beta/models")
));

// DeepSeek with custom endpoint
var client = new LLMClient(new DeepSeekProvider(
    apiKey: "your-api-key",
    model: "deepseek-chat",
    endpoint: new Uri("https://api.deepseek.com/v1/chat/completions")
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

