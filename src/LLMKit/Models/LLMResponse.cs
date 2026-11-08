namespace LLMKit.Models;

/// <summary>
/// Represents a response from an LLM provider
/// </summary>
public class LLMResponse
{
    /// <summary>
    /// The generated text response from the LLM
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The model's reasoning or thought summary, when the provider returns one. Otherwise null.
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// The token counts reported by the provider, or null if the provider returned none.
    /// </summary>
    public TokenUsage? Usage { get; set; }

    /// <summary>
    /// Why the model stopped, or null if the provider did not report it. <see cref="Models.FinishReason.MaxTokens"/>
    /// means the answer is cut off, or empty when the reasoning used all the tokens.
    /// </summary>
    public FinishReason? FinishReason { get; set; }
}