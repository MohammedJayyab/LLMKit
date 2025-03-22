# Examples

This page provides various examples of using LLMKit in different scenarios.

## Basic Usage

### Simple Text Generation
```csharp
using LLMKit;
using LLMKit.Providers;

using var client = new LLMClient(
    new OpenAIProvider(apiKey: "your-api-key", model: "gpt-3.5-turbo"),
    new ConversationService()
);

string response = await client.GenerateTextAsync(
    "You are a helpful assistant.",
    "What is the capital of France?"
);
```

### Conversation Management
```csharp
using var client = new LLMClient(
    new OpenAIProvider("your-api-key", "gpt-3.5-turbo"),
    new ConversationService()
);

var conversation = client.StartConversation();

await client.SendMessageAsync("Hello, how are you?");
await client.SendMessageAsync("What's the weather like?");
await client.SendMessageAsync("Tell me a joke");

string history = client.GetFormattedConversation();
```

## Advanced Usage

### Custom Parameters
```csharp
var client = new LLMClient(
    new OpenAIProvider("your-api-key", "gpt-3.5-turbo"),
    new ConversationService(),
    defaultMaxTokens: 1000,
    defaultTemperature: 0.7
);
```

### Custom Conversation Service
```csharp
public class CustomConversationService : IConversationService
{
    private readonly ILogger<CustomConversationService> _logger;

    public CustomConversationService(ILogger<CustomConversationService> logger)
    {
        _logger = logger;
    }

    public Conversation CreateConversation(int maxMessages)
    {
        _logger.LogInformation("Creating new conversation with max messages: {MaxMessages}", maxMessages);
        return new Conversation(maxMessages);
    }

    public void AddMessage(Conversation conversation, string role, string content)
    {
        _logger.LogInformation("Adding message with role: {Role}", role);
        conversation.AddMessage(role, content);
    }

    public void ClearConversation(Conversation conversation)
    {
        _logger.LogInformation("Clearing conversation");
        conversation.Clear();
    }
}

// Usage
using var client = new LLMClient(
    new OpenAIProvider("your-api-key", "gpt-3.5-turbo"),
    new CustomConversationService(logger)
);
```

### Custom Message Building
```csharp
var builder = ChatMessageBuilder.Create()
    .AddSystemMessage("You are a helpful assistant.")
    .AddUserMessage("What is the capital of France?")
    .AddAssistantMessage("The capital of France is Paris.")
    .AddUserMessage("What is its population?");

var messages = builder.Build();
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
services.AddSingleton<IConversationService, ConversationService>();
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
            var response = await _client.SendMessageAsync(request.Message);
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
    var response = await client.GenerateTextAsync(
        "You are a helpful assistant.",
        "What is the capital of France?"
    );
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
        "You are a helpful assistant.",
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
    ),
    new ConversationService()
);

// Gemini with custom endpoint
var client = new LLMClient(
    new GeminiProvider(
        apiKey: "your-api-key",
        model: "gemini-pro",
        endpoint: new Uri("https://generativelanguage.googleapis.com/v1beta/models")
    ),
    new ConversationService()
);
```

## Testing Examples

### Unit Testing
```csharp
public class LLMClientTests
{
    private readonly Mock<ILLMProvider> _providerMock;
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly LLMClient _client;

    public LLMClientTests()
    {
        _providerMock = new Mock<ILLMProvider>();
        _conversationServiceMock = new Mock<IConversationService>();
        _client = new LLMClient(
            _providerMock.Object,
            _conversationServiceMock.Object
        );
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
        var response = await _client.GenerateTextAsync(
            "System message",
            "User message"
        );

        // Assert
        Assert.Equal("Test response", response);
        _providerMock.Verify(p => p.GenerateTextAsync(
            It.IsAny<LLMRequest>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
``` 