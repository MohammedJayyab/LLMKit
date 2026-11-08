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
- Multimodal support (text + images)
- Reasoning / thinking effort control and access to the returned reasoning
- ✨ **NEW in 4.0.0!** Per-model reasoning: each model gets only the reasoning parameters and
  values it accepts (GPT-6, Claude 3.7 to 5.x, Gemini 2.5 to 4, Gemma 4)
- ✨ **NEW in 4.0.0!** Token usage, finish reason (complete, cut off or blocked) and model
  capabilities for every response; `maxTokens` is no longer capped at 4096
- ✨ **NEW in 4.0.0!** IntelliSense documentation and a symbol package for debugging into LLMKit
- Anthropic (Claude) provider
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
- No other package dependencies
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

`maxTokens` is the maximum length of each response: the reasoning plus the answer. Your question and
the conversation history are not counted. The default is 4096. LLMKit has no upper limit, so you can
set what the model allows (for example 16000 or 32000); a value above the model's limit is rejected by
the provider with an error. A higher value lets reasoning models think without leaving the answer
empty, but long responses take longer: requests time out after 3 minutes (OpenAI, Anthropic,
DeepSeek), 2 minutes (Gemini) or 60 seconds (LLM Studio).

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

### Token Usage
```csharp
string response = await client.GenerateTextAsync("What is the capital of France?");

TokenUsage? usage = client.LastUsage; // null if the provider returned no usage
Console.WriteLine($"Input: {usage?.InputTokens}, output: {usage?.OutputTokens}, " +
    $"reasoning: {usage?.ReasoningTokens}, total: {usage?.TotalTokens}");
```

The counts come from the provider's response and mean the same for every provider. `OutputTokens`
includes the reasoning tokens. `ReasoningTokens` is the part of it spent on reasoning, or null when
the provider does not report it separately (Anthropic). The usage is also set when the answer is empty
because the reasoning used up `maxTokens`.

### Cut-off or Empty Answers
`client.LastFinishReason` tells you why the model stopped, so you can explain an empty or cut-off
answer instead of leaving the user guessing:

```csharp
string answer = await client.GenerateTextAsync("Explain quantum computing.");

switch (client.LastFinishReason)
{
    case FinishReason.MaxTokens when string.IsNullOrEmpty(answer):
        Console.WriteLine("No answer: the reasoning used all the tokens. Increase maxTokens or lower the reasoning effort.");
        break;
    case FinishReason.MaxTokens:
        Console.WriteLine(answer + "\n[Answer cut off: increase maxTokens for the full answer.]");
        break;
    case FinishReason.ContentFilter:
        Console.WriteLine("The provider's safety filter blocked this answer.");
        break;
    default:
        Console.WriteLine(answer);
        break;
}
```

| FinishReason | Meaning |
|---|---|
| `Completed` | The answer is complete |
| `MaxTokens` | Stopped at `maxTokens`: cut off, or empty when the reasoning used all the tokens |
| `ContentFilter` | Blocked or stopped by the provider's safety filter |
| `Other` | Any other reason the provider reports |
| `null` | The provider did not report a reason |

LLMKit does not throw for these cases: a cut-off answer is often still useful, so your code decides.
The same value is on `LLMResponse.FinishReason` when you call a provider directly.

An empty answer is not added to the conversation, so the conversation stays valid and the user can
simply ask again (Anthropic and Gemini reject empty messages in the history).

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

LLMKit recognizes each model's generation from its name and only sends the parameters and values
that model accepts. Models that do not reason get no reasoning parameters at all.

| Provider | Effort is sent as | Reasoning returned |
|---|---|---|
| OpenAI | `reasoning_effort` (GPT-5 and later, o-series) | No (Chat Completions does not return it) |
| Anthropic | Claude 4.6 and later: adaptive `thinking` plus `output_config.effort`. Claude 3.7 to 4.5: `thinking` with `budget_tokens` | Yes, thought summaries |
| Gemini | `thinking_budget` for 2.5 models, `thinking_level` for Gemini 3 and later and Gemma 4 | Yes, thought summaries |
| DeepSeek | `thinking` on/off plus `reasoning_effort` | Yes, `reasoning_content` |
| LLM Studio | `reasoning_effort` | Yes, `reasoning` or `reasoning_content` |

Notes:
- `None` turns reasoning off where the model allows it, otherwise it uses the lowest level the
  model offers:
  - OpenAI: `minimal` on GPT-5 (5.0), `low` on the o-series, GPT-6 Astra and GPT-6.1 Sol.
  - Gemini: `low` on models without `minimal`; budget 128 on Gemini 2.5 Pro.
- A level a model does not offer is sent as the next higher one. For example, Gemini 3 Pro has no
  `medium`, so Medium is sent as `high`.
- Reasoning models do not get `temperature` while they think. On OpenAI, `top_p` and the penalties
  are dropped too.
- Anthropic: `None` leaves thinking unset and uses `low` effort where the model supports effort.
  Claude 3.7 to 4.5 thinking budgets are Low = 1024, Medium = 8192, High = 24576 tokens, kept at
  least 1024 tokens below `maxTokens`. With the default 4096 that gives 1024 / 3072 / 3072; with
  `maxTokens` 32000 the full 1024 / 8192 / 24576.
- Gemini 2.5 budgets: Low = 1024, Medium = 8192, High = 24576 tokens.
- DeepSeek maps medium to high itself and ignores sampling parameters while thinking. Current
  DeepSeek models think by default when no effort is set.
- LLM Studio's `reasoning_effort` support depends on the server version (0.4.8 or later) and model.
- Thinking tokens count toward `maxTokens` (default 4096). At high effort a long reasoning can use
  it up and leave the answer empty; set a higher `maxTokens` (for example 16000) for reasoning models.

### Checking what a model supports

`client.Capabilities` tells you what LLMKit does for the client's model with its current settings:

```csharp
using var client = new LLMClient(
    new OpenAIProvider("your-api-key", "gpt-6-luna"),
    reasoningEffort: ReasoningEffort.Low
);

ProviderCapabilities caps = client.Capabilities;
caps.SupportsReasoningEffort // true:  the effort is sent
caps.CanTurnOffReasoning     // true:  ReasoningEffort.None turns reasoning off
caps.SupportsTemperature     // false: not sent while the model reasons (true with ReasoningEffort.None)
caps.ReturnsReasoningText    // false: OpenAI Chat Completions does not return the reasoning
caps.SupportsImages          // true:  images are sent (whether the model accepts them is up to the model)
```

The values follow from the model name, the reasoning effort and `maxTokens`. For a model name LLMKit
does not know they are a best guess.

## Supported Providers

To list the providers available in your LLMKit version:

```csharp
IReadOnlyList<string> providers = LLMClient.GetSupportedProviders();
// Anthropic, DeepSeek, Gemini, LLMStudio, OpenAI
```

### OpenAI
```csharp
var provider = new OpenAIProvider(
    apiKey: "your-api-key",
    model: "gpt-3.5-turbo"
);
```

**Reasoning models (GPT-5 and later, o-series):** these models reject `temperature`, `top_p` and
the penalty parameters, so LLMKit omits them automatically. The exception is reasoning effort
`None` on models that can turn reasoning off (GPT-5.1 and later, except GPT-6 Astra and GPT-6.1
Sol). Reasoning tokens count against `maxTokens`, so use a generous value or the response text may
come back empty. See [Reasoning / Thinking](#reasoning--thinking).

`chat-latest` reasons at a fixed effort and accepts only the default temperature, so LLMKit sends
neither the reasoning effort nor the sampling parameters to it.

If LLMKit cannot tell from the name whether a model reasons (for example a deployment behind an
OpenAI-compatible endpoint), say so explicitly:

```csharp
var provider = new OpenAIProvider(
    apiKey: "your-api-key",
    model: "my-reasoning-deployment",
    endpoint: new Uri("https://my-endpoint.example.com/v1/chat/completions"),
    isReasoningModel: true
);
```

Models that only work with the Responses API (`*-codex`, `*-pro`) are not supported, because
LLMKit uses Chat Completions.

### Anthropic (Claude)
```csharp
var provider = new AnthropicProvider(
    apiKey: "your-api-key",
    model: "claude-opus-5-5"
);
```

Newer Claude models (Opus 4.7 and later, Sonnet 5, Fable, Haiku 5.5) reject `temperature`, so LLMKit
omits it for them. Older models get `temperature` only while thinking is off. Sampling parameters
other than `temperature` are never sent to Anthropic.

### Gemini
```csharp
var provider = new GeminiProvider(
    apiKey: "your-api-key",
    model: "gemini-3.6-flash"
);
```

### DeepSeek
```csharp
var provider = new DeepSeekProvider(
    apiKey: "your-api-key",
    model: "deepseek-flash"
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

If your server requires authentication, pass the key as `apiKey`; it is sent as a Bearer token.
(Before 4.0.0 the key was ignored.)

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
    model: "gemini-3.6-flash",
    endpoint: new Uri("https://generativelanguage.googleapis.com/v1beta/models")
));

// DeepSeek with custom endpoint
var client = new LLMClient(new DeepSeekProvider(
    apiKey: "your-api-key",
    model: "deepseek-flash",
    endpoint: new Uri("https://api.deepseek.com/v1/chat/completions")
));

// LLM Studio with custom endpoint
var client = new LLMClient(new LLMStudioProvider(
    apiKey: null,
    model: "glm-4.6v-flash",
    endpoint: new Uri("http://localhost:1234/v1/chat/completions")
));
```

## Upgrading from 3.x to 4.0.0

Most code keeps working unchanged. Check these four changes:

- **Provider response models are internal.** `OpenAIResponse`, `AnthropicResponse`,
  `GeminiResponse`, `DeepSeekResponse`, `LLMStudioResponse` and their types (`Choice`, `Message`,
  `Candidate`, `Content`, `Part`, ...) are no longer public. Read the result from `LLMResponse`
  (`Text`, `Reasoning`, `Usage`, `FinishReason`) or from `LLMClient` instead.
- **`maxTokens` above 4096 is sent as set.** Before, it was silently lowered to 4096. If you pass a
  value above the model's limit, the provider now rejects the request with an error.
- **`LastReasoning` is null after a failed call.** Before, it kept the previous call's value.
  `LastUsage` and `LastFinishReason` behave the same.
- **No more `System.Text.Json` package dependency.** LLMKit uses the `System.Text.Json` built into
  .NET. If your app uses `System.Text.Json` 9 or later features and only got that package through
  LLMKit, add a `PackageReference` to it in your own project.

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

