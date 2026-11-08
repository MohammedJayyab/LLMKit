using System.Diagnostics;
using System.Globalization;
using LLMKit;
using LLMKit.Exceptions;
using LLMKit.Models;
using LLMKit.Providers;
using Microsoft.Extensions.Configuration;

// Setup: copy appsettings.example.json to appsettings.Local.json (git-ignored) and fill in your API keys.
// Every provider with a "Model" set is offered in the menu. Settings per provider (null or left out: the default):
//   ApiKey            required, except for LLMStudio
//   Model             the model name
//   Endpoint          the provider's standard URL (Gemini: a custom URL must include the model path)
//   IsReasoningModel  OpenAI only: true or false; detected from the model name by default
//   ReasoningEffort   None, Low, Medium or High; the model's default by default
//   MaxTokens         maximum response length, reasoning plus answer; the question is not counted
//                     (default 4096; no upper limit in LLMKit, the model's own limit applies)
//   Temperature       0.0 to 2.0 (default 0.3); not sent to models that reject it
//   MaxMessages       messages kept in the conversation (default 15)
//   SystemMessage     instructions for the model; LLMKit's built-in message by default
const int DefaultMaxTokens = 4096;
const double DefaultTemperature = 0.3;
const int DefaultMaxMessages = 15;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .Build();

(string Name, Func<string, string, Uri?, ILLMProvider> Create)[] supportedProviders =
{
    ("OpenAI", (apiKey, model, endpoint) =>
        bool.TryParse(configuration["OpenAI:IsReasoningModel"], out bool isReasoningModel)
            ? new OpenAIProvider(apiKey, model, endpoint, isReasoningModel)
            : new OpenAIProvider(apiKey, model, endpoint)),
    ("Anthropic", (apiKey, model, endpoint) => new AnthropicProvider(apiKey, model, endpoint)),
    ("Gemini", (apiKey, model, endpoint) => new GeminiProvider(apiKey, model, endpoint)),
    ("DeepSeek", (apiKey, model, endpoint) => new DeepSeekProvider(apiKey, model, endpoint)),
    ("LLMStudio", (apiKey, model, endpoint) =>
        new LLMStudioProvider(string.IsNullOrWhiteSpace(apiKey) ? null : apiKey, model, endpoint)),
};

var configured = supportedProviders
    .Where(p => !string.IsNullOrWhiteSpace(configuration[$"{p.Name}:Model"]))
    .ToArray();

if (configured.Length == 0)
{
    Console.WriteLine("No provider is configured. Copy appsettings.example.json to appsettings.Local.json and fill in your settings.");
    return;
}

Console.WriteLine("Choose a provider:");
for (int i = 0; i < configured.Length; i++)
{
    Console.WriteLine($"  {i + 1}. {configured[i].Name} ({configuration[$"{configured[i].Name}:Model"]})");
}

int choice;
do
{
    Console.Write($"Number (1-{configured.Length}): ");
    string? input = Console.ReadLine();
    if (input == null)
    {
        return;
    }
    int.TryParse(input, out choice);
}
while (choice < 1 || choice > configured.Length);

var selected = configured[choice - 1];
var settings = configuration.GetSection(selected.Name);

string apiKey = settings["ApiKey"] ?? string.Empty;
string model = settings["Model"]!;
ReasoningEffort? reasoningEffort =
    Enum.TryParse(settings["ReasoningEffort"], ignoreCase: true, out ReasoningEffort effort) ? effort : null;
int maxTokens = int.TryParse(settings["MaxTokens"], out int tokens) ? tokens : DefaultMaxTokens;
double temperature =
    double.TryParse(settings["Temperature"], NumberStyles.Float, CultureInfo.InvariantCulture, out double temp)
        ? temp : DefaultTemperature;
int maxMessages = int.TryParse(settings["MaxMessages"], out int messages) ? messages : DefaultMaxMessages;
string? systemMessage = settings["SystemMessage"];

string? endpointValue = settings["Endpoint"];
Uri? endpoint = null;
if (!string.IsNullOrWhiteSpace(endpointValue) && !Uri.TryCreate(endpointValue, UriKind.Absolute, out endpoint))
{
    Console.WriteLine($"Configuration error for {selected.Name}: Endpoint '{endpointValue}' is not a valid URL.");
    return;
}

using var client = CreateClient();
if (client == null)
{
    return;
}

if (!string.IsNullOrWhiteSpace(systemMessage))
{
    client.SetSystemMessage(systemMessage);
}

var capabilities = client.Capabilities;
string temperatureText = capabilities.SupportsTemperature ? temperature.ToString(CultureInfo.InvariantCulture) : "not used";
Console.WriteLine($"Provider: {selected.Name}, model: {model}, reasoning effort: {reasoningEffort?.ToString() ?? "model default"}, " +
    $"max tokens: {maxTokens}, temperature: {temperatureText}, max messages: {maxMessages}");
Console.WriteLine($"Capabilities: reasoning effort {YesNo(capabilities.SupportsReasoningEffort)}, " +
    $"can turn off reasoning {YesNo(capabilities.CanTurnOffReasoning)}, reasoning text {YesNo(capabilities.ReturnsReasoningText)}, " +
    $"images {YesNo(capabilities.SupportsImages)}");

static string YesNo(bool value) => value ? "yes" : "no";

// Invalid settings (missing key, MaxMessages below 0, ...) are reported instead of crashing the demo.
// The client disposes the provider; if the client cannot be created, the provider is disposed here.
LLMClient? CreateClient()
{
    ILLMProvider? provider = null;
    try
    {
        provider = selected.Create(apiKey, model, endpoint);
        return new LLMClient(provider, maxTokens, temperature, maxMessages, reasoningEffort);
    }
    catch (ArgumentException ex)
    {
        provider?.Dispose();
        Console.WriteLine($"Configuration error for {selected.Name}: {ex.Message}");
        return null;
    }
}

// Explains an empty, cut-off or blocked answer, so the user is not left guessing
void PrintFinishNote(FinishReason? reason, string answer, TokenUsage? usage)
{
    string? note = reason switch
    {
        FinishReason.MaxTokens when string.IsNullOrWhiteSpace(answer) =>
            $"No answer: the model used all its tokens{(usage?.ReasoningTokens > 0 ? " for reasoning" : "")} " +
            $"(MaxTokens = {maxTokens}). Increase MaxTokens or lower ReasoningEffort.",
        FinishReason.MaxTokens =>
            $"The answer was cut off at MaxTokens ({maxTokens}). Increase MaxTokens for the full answer.",
        FinishReason.ContentFilter => "The provider's safety filter blocked or stopped this answer.",
        _ => null
    };

    if (note != null)
    {
        Console.WriteLine($"[{note}]");
    }
}

void PrintStatistics(TimeSpan elapsed, string answer, string? reasoningText, TokenUsage? usage, FinishReason? finishReason,
    int messageCount)
{
    var invariant = CultureInfo.InvariantCulture;
    Console.WriteLine("--- Statistics ---");
    Console.WriteLine($"Provider:       {selected.Name} ({model})");
    Console.WriteLine($"Response time:  {elapsed.TotalSeconds.ToString("0.00", invariant)} s");
    Console.WriteLine($"Finish reason:  {finishReason?.ToString() ?? "not reported"}");

    if (usage == null)
    {
        Console.WriteLine("Tokens:         not reported by the provider");
    }
    else
    {
        string outputDetail = usage.ReasoningTokens is { } reasoningTokens
            ? $" (reasoning: {reasoningTokens}, answer: {usage.OutputTokens - reasoningTokens})"
            : " (reasoning: not reported separately)";
        Console.WriteLine($"Input tokens:   {usage.InputTokens}");
        Console.WriteLine($"Output tokens:  {usage.OutputTokens}{outputDetail}");
        Console.WriteLine($"Total tokens:   {usage.TotalTokens}");
        if (elapsed.TotalSeconds > 0)
        {
            Console.WriteLine($"Output speed:   {(usage.OutputTokens / elapsed.TotalSeconds).ToString("0.0", invariant)} tokens/s");
        }
    }

    Console.WriteLine($"Answer length:  {answer.Length} characters");
    Console.WriteLine($"Reasoning text: {(string.IsNullOrWhiteSpace(reasoningText) ? "none" : $"{reasoningText.Length} characters")}");
    Console.WriteLine($"Conversation:   {messageCount} of {maxMessages} messages");
    Console.WriteLine("------------------");
}
Console.WriteLine("Enter your message (type 'exit' to quit).");

while (true)
{
    Console.Write("You: ");
    string? userInput = Console.ReadLine();

    if (userInput == null || userInput.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    try
    {
        var stopwatch = Stopwatch.StartNew();
        string response = await client.GenerateTextAsync(userInput);
        stopwatch.Stop();

        string? reasoningText = client.LastReasoning;
        if (!string.IsNullOrWhiteSpace(reasoningText))
        {
            Console.WriteLine($"Reasoning: {reasoningText}");
        }

        Console.WriteLine($"LLM: {response}");
        PrintFinishNote(client.LastFinishReason, response, client.LastUsage);
        PrintStatistics(stopwatch.Elapsed, response, reasoningText, client.LastUsage, client.LastFinishReason,
            client.CurrentConversation.MessageCount);
    }
    catch (LLMException ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
