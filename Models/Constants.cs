namespace LLMKit.Models
{
    public static class Constants
    {
        // API Endpoints
        public const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

        public const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models";
        public const string DeepSeekEndpoint = "https://api.deepseek.com/v1/chat/completions";

        // HTTP Headers
        public const string BearerAuth = "Bearer";

        public const string JsonContentType = "application/json";
        public const string JsonAccept = "application/json";

        // Default Values
        public const double DefaultTemperature = 0.3;

        public const double DefaultTopP = 1.0;
        public const double DefaultFrequencyPenalty = 0.0;
        public const double DefaultPresencePenalty = 0.0;
        public const int DefaultMaxTokens = 2024;
        public const bool DefaultStream = false;

        // Validation Boundaries
        public const double MinTemperature = 0.0;

        public const double MaxTemperature = 2.0;
        public const double MinTopP = 0.0;
        public const double MaxTopP = 1.0;
        public const double MinPenalty = -2.0;
        public const double MaxPenalty = 2.0;
        public const int MinTokens = 1;
        public const int MaxTokens = 4096;
    }
}