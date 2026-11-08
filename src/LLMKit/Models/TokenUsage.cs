namespace LLMKit.Models;

/// <summary>
/// The token counts the provider reported for one request.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Tokens in the request: system message, conversation history and images.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Tokens the model generated, including reasoning tokens.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// The part of <see cref="OutputTokens"/> spent on reasoning, when the provider reports it separately. Otherwise null.
    /// </summary>
    public int? ReasoningTokens { get; set; }

    /// <summary>
    /// <see cref="InputTokens"/> plus <see cref="OutputTokens"/>.
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;
}
