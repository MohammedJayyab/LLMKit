namespace LLMKit.Models
{
    internal static class Constants
    {
        // HTTP Headers
        public const string BearerAuth = "Bearer";

        public const string JsonContentType = "application/json";
        public const string JsonAccept = "application/json";
        public const string SystemMessage = "You are a helpful assistant that can answer questions and help with tasks.";

        // Default Values
        public const double DefaultTemperature = 0.3;

        public const double DefaultTopP = 1.0;
        public const double DefaultFrequencyPenalty = 0.0;
        public const double DefaultPresencePenalty = 0.0;
        public const int DefaultMaxTokens = 4096;
        public const bool DefaultStream = false;
        public const int DefaultMaxMessages = 15;

        // Validation Boundaries
        public const double MinTemperature = 0.0;

        public const double MaxTemperature = 2.0;
        public const double MinTopP = 0.0;
        public const double MaxTopP = 1.0;
        public const double MinPenalty = -2.0;
        public const double MaxPenalty = 2.0;
        public const int MinTokens = 1;
    }
}