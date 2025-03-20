namespace LLMKit.Exceptions
{
    public class LLMException : Exception
    {
        public LLMException(string message) : base(message) { }
        public LLMException(string message, Exception innerException) : base(message, innerException) { }
    }
} 