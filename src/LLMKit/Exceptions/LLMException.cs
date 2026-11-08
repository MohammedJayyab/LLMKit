namespace LLMKit.Exceptions
{
    /// <summary>Thrown when a request to an LLM provider fails. The message includes the provider's error response.</summary>
    public class LLMException : Exception
    {
        /// <summary>Creates the exception with a message.</summary>
        public LLMException(string message) : base(message) { }
        /// <summary>Creates the exception with a message and the exception that caused it.</summary>
        public LLMException(string message, Exception innerException) : base(message, innerException) { }
    }
}