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
}