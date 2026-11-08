namespace LLMKit.Models
{
    /// <summary>
    /// How much reasoning ("thinking") a model should do before answering.
    /// Each provider maps this to its own setting; leave unset to use the model's default.
    /// </summary>
    public enum ReasoningEffort
    {
        /// <summary>Turn reasoning off, or use the lowest level the model allows.</summary>
        None,
        /// <summary>A little reasoning: faster and cheaper.</summary>
        Low,
        /// <summary>A balance between speed and answer quality.</summary>
        Medium,
        /// <summary>The most reasoning: slower, uses more tokens.</summary>
        High
    }

    internal static class ReasoningEffortExtensions
    {
        /// <summary>
        /// Gets the lowercase value used by the OpenAI-style APIs ("none", "low", "medium", "high").
        /// </summary>
        public static string ToApiValue(this ReasoningEffort effort) => effort.ToString().ToLowerInvariant();
    }
}
