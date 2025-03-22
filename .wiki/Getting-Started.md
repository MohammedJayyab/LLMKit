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

string response = await client.GenerateTextAsync(
    "You are a helpful assistant.",
    "What is the capital of France?"
);
```

### Conversation Management
```csharp
using var client = new LLMClient(
    new OpenAIProvider("your-api-key", "gpt-3.5-turbo")
);

var conversation = client.StartConversation();

await client.SendMessageAsync("Hello, how are you?");
await client.SendMessageAsync("What's the weather like?");
await client.SendMessageAsync("Tell me a joke");

string history = client.GetFormattedConversation();
```

## Configuration

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

## Error Handling

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

## Cancellation Support

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var response = await client.GenerateTextAsync(
    "You are a helpful assistant.",
    "What is the capital of France?",
    cts.Token
);
```

## Next Steps

- Check out our [Examples](Examples) page for more detailed usage scenarios
- Read the [API Reference](API-Reference) for detailed documentation
- Learn about the [Architecture](Architecture) to understand how LLMKit works
- Consider [Contributing](Contributing) to the project 