namespace LLMKit.Exceptions
{
    public class LLMKitException : Exception
    {
        public LLMKitException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}