# Examples

This page provides various examples of using LLMKit in different scenarios.

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

## Advanced Usage

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

### Building Messages Manually
```csharp
// Creating a chat message with text only
var textMessage = new ChatMessage(ChatMessage.Roles.User, "What is the capital of France?");

// Creating a chat message with text and image
var multimodalMessage = new ChatMessage(ChatMessage.Roles.User, "What's in this image?");
multimodalMessage.AddImage("path/to/image.jpg");
```

## Integration Examples

### ASP.NET Core Integration
```csharp
// Program.cs
services.AddSingleton<ILLMProvider>(sp => 
    new OpenAIProvider(
        apiKey: Configuration["OpenAI:ApiKey"],
        model: Configuration["OpenAI:Model"]
    )
);
services.AddSingleton<LLMClient>();

// Controller
public class ChatController : ControllerBase
{
    private readonly LLMClient _client;

    public ChatController(LLMClient client)
    {
        _client = client;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            var response = await _client.GenerateTextAsync(request.Message);
            return Ok(new { response });
        }
        catch (LLMException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

### Error Handling
```csharp
try
{
    var response = await client.GenerateTextAsync("What is the capital of France?");
    Console.WriteLine(response);
}
catch (LLMException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // The message includes the provider's error response (for example an invalid model or parameter)

    // Handle specific error cases
    var inner = ex.GetBaseException();
    if (inner is HttpRequestException http)
    {
        // Handle HTTP and network errors (http.StatusCode is set when the server responded)
    }
    else if (inner is JsonException)
    {
        // Handle parsing errors
    }
}
```

### Cancellation Support
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var response = await client.GenerateTextAsync(
        "What is the capital of France?",
        cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

### Custom Endpoints
```csharp
// OpenAI with custom endpoint
var client = new LLMClient(
    new OpenAIProvider(
        apiKey: "your-api-key",
        model: "gpt-3.5-turbo",
        endpoint: new Uri("https://api.openai.com/v1/chat/completions")
    )
);

// Anthropic with custom endpoint
var client = new LLMClient(
    new AnthropicProvider(
        apiKey: "your-api-key",
        model: "claude-opus-5-5",
        endpoint: new Uri("https://api.anthropic.com/v1/messages")
    )
);

// Gemini with custom endpoint
var client = new LLMClient(
    new GeminiProvider(
        apiKey: "your-api-key",
        model: "gemini-2.0-flash",
        endpoint: new Uri("https://generativelanguage.googleapis.com/v1beta/models")
    )
);

// LLM Studio (Local Models) with custom endpoint
var client = new LLMClient(
    new LLMStudioProvider(
        apiKey: null,  // Optional for local servers
        model: "glm-4.6v-flash",
        endpoint: new Uri("http://localhost:1234/v1/chat/completions")
    )
);
```

## Reasoning / Thinking

### Setting the reasoning effort
```csharp
using LLMKit.Models; // ReasoningEffort

using var client = new LLMClient(
    new AnthropicProvider(apiKey: "your-api-key", model: "claude-opus-5-5"),
    reasoningEffort: ReasoningEffort.High
);

string answer = await client.GenerateTextAsync("Explain the Monty Hall problem.");

// The reasoning returned with the last response, or null if the provider returned none
Console.WriteLine(client.LastReasoning);
```

### Calling a provider directly
```csharp
var request = new LLMRequest()
    .WithMessages(messages)
    .WithReasoningEffort(ReasoningEffort.Low);

LLMResponse response = await provider.GenerateTextAsync(request);
Console.WriteLine(response.Text);
Console.WriteLine(response.Reasoning);
```

### Turning reasoning off on OpenAI
```csharp
// With effort None, GPT-5.1 and later accept temperature and the other sampling parameters again
using var client = new LLMClient(
    new OpenAIProvider(apiKey: "your-api-key", model: "gpt-5.2"),
    reasoningEffort: ReasoningEffort.None
);
```

## Local Model Support (LLM Studio)

### Using Local LLM Models
```csharp
// Connect to a local LLM Studio server
using var client = new LLMClient(
    new LLMStudioProvider(
        apiKey: null,  // No authentication needed for local servers
        model: "glm-4.6v-flash"
    )
);

// Use it just like any other provider
string response = await client.GenerateTextAsync("What is the capital of France?");
Console.WriteLine(response);
```

### Local Model with Custom Endpoint
```csharp
// If your LLM Studio server runs on a different port
using var client = new LLMClient(
    new LLMStudioProvider(
        apiKey: null,
        model: "glm-4.6v-flash",
        endpoint: new Uri("http://localhost:8080/v1/chat/completions")
    )
);
```

## Testing Examples

### Unit Testing
```csharp
public class LLMClientTests
{
    private readonly Mock<ILLMProvider> _providerMock;
    private readonly LLMClient _client;

    public LLMClientTests()
    {
        _providerMock = new Mock<ILLMProvider>();
        _client = new LLMClient(_providerMock.Object);
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnResponse()
    {
        // Arrange
        _providerMock.Setup(p => p.GenerateTextAsync(
            It.IsAny<LLMRequest>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new LLMResponse { Text = "Test response" });

        // Act
        var response = await _client.GenerateTextAsync("Test message");

        // Assert
        Assert.Equal("Test response", response);
        _providerMock.Verify(p => p.GenerateTextAsync(
            It.IsAny<LLMRequest>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
``` 