# LLMKit

[![NuGet](https://img.shields.io/nuget/v/LLMKit.svg)](https://www.nuget.org/packages/LLMKit)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

LLMKit is a thread-safe .NET library designed to simplify interactions with various Large Language Models (LLMs) such as OpenAI, Gemini, and DeepSeek. It provides a unified interface for sending prompts and retrieving responses, allowing developers to easily switch between different LLM providers without significant code changes.

## Features

- **Provider Abstraction**: A consistent interface for interacting with multiple LLM providers
- **Thread-Safe Implementation**: Safe for use in multi-threaded environments
- **Simplified Client**: An `LLMClient` that handles the complexities of LLM interactions
- **Fluent Message Building**: A `ChatMessageBuilder` for easy creation of message lists
- **Simplified Conversations**: A `GenerateTextAsync` method for quick and easy conversations
- **Configurable Parameters**: Set defaults for `MaxTokens`, `Temperature`, `TopP`, and more
- **Robust Error Handling**: Comprehensive exception handling with provider-specific context
- **Dependency Injection Friendly**: Designed to work seamlessly with .NET dependency injection
- **Cancellation Support**: Built-in support for operation cancellation

## Requirements

- .NET 8.0 or later
- Valid API keys for the LLM providers you want to use

## Installation

You can install LLMKit via NuGet:

```powershell
Install-Package LLMKit
```

## Quick Start

```csharp
using LLMKit;
using LLMKit.Providers;

// Initialize with OpenAI
var client = new LLMClient(
    new OpenAIProvider(apiKey: "your-api-key", model: "gpt-3.5-turbo")
);

// Generate a response
string response = await client.GenerateTextAsync(
    "You are a helpful assistant.",
    "What is the capital of France?"
);
```

## Advanced Usage

### Configuration with Dependency Injection

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILLMProvider>(sp => 
            new OpenAIProvider(
                apiKey: Configuration["OpenAI:ApiKey"],
                model: Configuration["OpenAI:Model"]
            )
        );
        services.AddSingleton<LLMClient>();
    }
}
```

### Custom Parameters

```csharp
var request = new LLMRequest()
    .WithMaxTokens(1000)
    .WithTemperature(0.7)
    .WithTopP(0.9)
    .WithFrequencyPenalty(0.5)
    .WithPresencePenalty(0.5)
    .WithStream(true);
```

### Thread-Safe Usage

```csharp
// The LLMClient is thread-safe and can handle concurrent requests
var client = new LLMClient(
    new OpenAIProvider(apiKey: "your-api-key", model: "gpt-3.5-turbo")
);

// Example of concurrent requests
var tasks = new[]
{
    client.GenerateTextAsync("You are a helpful assistant.", "What is the capital of France?"),
    client.GenerateTextAsync("You are a code expert.", "Write a C# function to calculate factorial."),
    client.GenerateTextAsync("You are a math tutor.", "Explain the Pythagorean theorem.")
};

// All requests are processed concurrently and safely
var responses = await Task.WhenAll(tasks);
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
catch (LLMKitException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
    }
}
```

### Cancellation Support

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
try
{
    var response = await client.GenerateTextAsync(
        "You are a helpful assistant.",
        "What is the capital of France?",
        cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled.");
}
```

## Supported Providers

### OpenAI
- Supports GPT models
- Uses chat completions API
- Configurable parameters

### Gemini
- Supports Google's Gemini models
- Automatic system message handling
- Custom request formatting

### DeepSeek
- Supports DeepSeek's chat models
- Standard chat completions API
- Configurable parameters

## Performance Considerations

- The library is designed for thread-safe operations
- HTTP connections are managed efficiently
- Request parameters are validated and clamped to safe ranges
- Memory usage is optimized for large responses

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues. When contributing:

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Create a new Pull Request

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Support

If you encounter any issues or have questions, please open an issue in the GitHub repository.