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
}