namespace LLMKit.Models;

/// <summary>
/// Why the model stopped generating, with the same meaning for every provider.
/// </summary>
public enum FinishReason
{
    /// <summary>The answer is complete.</summary>
    Completed,

    /// <summary>
    /// The answer stopped at maxTokens: it is cut off, or empty when the reasoning used all the tokens.
    /// </summary>
    MaxTokens,

    /// <summary>The provider's safety filter blocked or stopped the answer.</summary>
    ContentFilter,

    /// <summary>Any other reason the provider reports, for example a tool call.</summary>
    Other
}

/// <summary>
/// Converts each provider's finish value to <see cref="FinishReason"/>. Null stays null (not reported).
/// </summary>
internal static class FinishReasonParser
{
    // OpenAI, DeepSeek and LLM Studio: finish_reason
    internal static FinishReason? FromChatCompletion(string? value) => value switch
    {
        null => null,
        "stop" => FinishReason.Completed,
        "length" => FinishReason.MaxTokens,
        "content_filter" => FinishReason.ContentFilter,
        _ => FinishReason.Other // tool_calls, function_call, insufficient_system_resource, ...
    };

    // Anthropic: stop_reason
    internal static FinishReason? FromAnthropic(string? value) => value switch
    {
        null => null,
        "end_turn" or "stop_sequence" => FinishReason.Completed,
        "max_tokens" => FinishReason.MaxTokens,
        "refusal" => FinishReason.ContentFilter,
        _ => FinishReason.Other // tool_use, pause_turn, ...
    };

    // Gemini: finishReason
    internal static FinishReason? FromGemini(string? value) => value switch
    {
        null => null,
        "STOP" => FinishReason.Completed,
        "MAX_TOKENS" => FinishReason.MaxTokens,
        "SAFETY" or "RECITATION" or "BLOCKLIST" or "PROHIBITED_CONTENT" or "SPII" or "IMAGE_SAFETY" => FinishReason.ContentFilter,
        _ => FinishReason.Other // LANGUAGE, MALFORMED_FUNCTION_CALL, OTHER, ...
    };
}
