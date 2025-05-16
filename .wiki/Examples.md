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
    // Handle specific error cases
    if (ex.InnerException is HttpRequestException)
    {
        // Handle network errors
    }
    else if (ex.InnerException is JsonException)
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

// Gemini with custom endpoint
var client = new LLMClient(
    new GeminiProvider(
        apiKey: "your-api-key",
        model: "gemini-2.0-flash",
        endpoint: new Uri("https://generativelanguage.googleapis.com/v1beta/models")
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