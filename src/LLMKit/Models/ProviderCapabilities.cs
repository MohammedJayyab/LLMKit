namespace LLMKit.Models;

/// <summary>
/// What LLMKit does for a provider's model with given request settings (reasoning effort, max tokens).
/// LLMKit recognizes models by name, so for a name it does not know these values are a best guess.
/// </summary>
public class ProviderCapabilities
{
    /// <summary>
    /// The reasoning effort is sent to the model. False for models that do not reason, and for models
    /// that reason at a fixed effort (for example OpenAI's chat-latest).
    /// </summary>
    public bool SupportsReasoningEffort { get; init; }

    /// <summary>
    /// <see cref="ReasoningEffort.None"/> turns reasoning off. False when the model always reasons
    /// (None then uses the lowest level) or does not reason at all.
    /// </summary>
    public bool CanTurnOffReasoning { get; init; }

    /// <summary>
    /// The temperature is applied with these settings. False when LLMKit leaves it out because the model
    /// rejects it (reasoning models while they think), or the provider ignores it (DeepSeek while thinking).
    /// </summary>
    public bool SupportsTemperature { get; init; }

    /// <summary>
    /// The reasoning text can be returned in <see cref="LLMResponse.Reasoning"/> with these settings.
    /// False means LLMKit returns none (for example OpenAI, whose Chat Completions API does not return it).
    /// </summary>
    public bool ReturnsReasoningText { get; init; }

    /// <summary>
    /// Images in messages are sent to the provider. Whether the model accepts them depends on the model.
    /// </summary>
    public bool SupportsImages { get; init; }
}
