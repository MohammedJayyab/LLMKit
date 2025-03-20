using System.Collections.ObjectModel;

namespace LLMKit.Models
{
    /// <summary>
    /// Represents a request to an LLM provider for text generation.
    /// Thread-safe implementation.
    /// </summary>
    public class LLMRequest
    {
        private readonly object _lock = new();
        private readonly List<ChatMessage> _messages;
        private double _temperature;
        private double _topP;
        private double _frequencyPenalty;
        private double _presencePenalty;
        private int _maxTokens;
        private bool _stream;

        /// <summary>
        /// Gets the list of chat messages in read-only format.
        /// </summary>
        public IReadOnlyList<ChatMessage> Messages
        {
            get
            {
                lock (_lock)
                {
                    return new ReadOnlyCollection<ChatMessage>(_messages.ToList());
                }
            }
            set
            {
                lock (_lock)
                {
                    _messages.Clear();
                    _messages.AddRange(value.ToList());
                }
            }
        }

        /// <summary>
        /// Gets the temperature value for text generation.
        /// </summary>
        public double Temperature
        {
            get
            {
                lock (_lock)
                {
                    return _temperature;
                }
            }
        }

        /// <summary>
        /// Gets the top-p value for text generation.
        /// </summary>
        public double TopP
        {
            get
            {
                lock (_lock)
                {
                    return _topP;
                }
            }
        }

        /// <summary>
        /// Gets the frequency penalty value.
        /// </summary>
        public double FrequencyPenalty
        {
            get
            {
                lock (_lock)
                {
                    return _frequencyPenalty;
                }
            }
        }

        /// <summary>
        /// Gets the presence penalty value.
        /// </summary>
        public double PresencePenalty
        {
            get
            {
                lock (_lock)
                {
                    return _presencePenalty;
                }
            }
        }

        /// <summary>
        /// Gets the maximum number of tokens to generate.
        /// </summary>
        public int MaxTokens
        {
            get
            {
                lock (_lock)
                {
                    return _maxTokens;
                }
            }
        }

        /// <summary>
        /// Gets whether streaming is enabled.
        /// </summary>
        public bool Stream
        {
            get
            {
                lock (_lock)
                {
                    return _stream;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of LLMRequest with default values.
        /// </summary>
        public LLMRequest()
        {
            _messages = new List<ChatMessage>();
            _temperature = Constants.DefaultTemperature;
            _topP = Constants.DefaultTopP;
            _frequencyPenalty = Constants.DefaultFrequencyPenalty;
            _presencePenalty = Constants.DefaultPresencePenalty;
            _maxTokens = Constants.DefaultMaxTokens;
            _stream = Constants.DefaultStream;
        }

        /// <summary>
        /// Sets the messages for the request.
        /// </summary>
        /// <param name="messages">The list of chat messages.</param>
        /// <returns>The current instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when messages is null.</exception>
        public LLMRequest WithMessages(IEnumerable<ChatMessage> messages)
        {
            Messages = messages.ToList();
            return this;
        }

        /// <summary>
        /// Sets the maximum number of tokens to generate.
        /// </summary>
        /// <param name="maxTokens">The maximum number of tokens.</param>
        /// <returns>The current instance for method chaining.</returns>
        public LLMRequest WithMaxTokens(int? maxTokens)
        {
            lock (_lock)
            {
                _maxTokens = maxTokens.HasValue
                    ? Math.Clamp(maxTokens.Value, Constants.MinTokens, Constants.MaxTokens)
                    : Constants.DefaultMaxTokens;
            }
            return this;
        }

        /// <summary>
        /// Sets the temperature value for text generation.
        /// </summary>
        /// <param name="temperature">The temperature value.</param>
        /// <returns>The current instance for method chaining.</returns>
        public LLMRequest WithTemperature(double? temperature)
        {
            lock (_lock)
            {
                _temperature = temperature.HasValue
                    ? Math.Clamp(temperature.Value, Constants.MinTemperature, Constants.MaxTemperature)
                    : Constants.DefaultTemperature;
            }
            return this;
        }

        /// <summary>
        /// Sets the top-p value for text generation.
        /// </summary>
        /// <param name="topP">The top-p value.</param>
        /// <returns>The current instance for method chaining.</returns>
        public LLMRequest WithTopP(double? topP)
        {
            lock (_lock)
            {
                _topP = topP.HasValue
                    ? Math.Clamp(topP.Value, Constants.MinTopP, Constants.MaxTopP)
                    : Constants.DefaultTopP;
            }
            return this;
        }

        /// <summary>
        /// Sets the frequency penalty value.
        /// </summary>
        /// <param name="frequencyPenalty">The frequency penalty value.</param>
        /// <returns>The current instance for method chaining.</returns>
        public LLMRequest WithFrequencyPenalty(double? frequencyPenalty)
        {
            lock (_lock)
            {
                _frequencyPenalty = frequencyPenalty.HasValue
                    ? Math.Clamp(frequencyPenalty.Value, Constants.MinPenalty, Constants.MaxPenalty)
                    : Constants.DefaultFrequencyPenalty;
            }
            return this;
        }

        /// <summary>
        /// Sets the presence penalty value.
        /// </summary>
        /// <param name="presencePenalty">The presence penalty value.</param>
        /// <returns>The current instance for method chaining.</returns>
        public LLMRequest WithPresencePenalty(double? presencePenalty)
        {
            lock (_lock)
            {
                _presencePenalty = presencePenalty.HasValue
                    ? Math.Clamp(presencePenalty.Value, Constants.MinPenalty, Constants.MaxPenalty)
                    : Constants.DefaultPresencePenalty;
            }
            return this;
        }

        /// <summary>
        /// Sets whether streaming is enabled.
        /// </summary>
        /// <param name="stream">Whether streaming is enabled.</param>
        /// <returns>The current instance for method chaining.</returns>
        public LLMRequest WithStream(bool? stream)
        {
            lock (_lock)
            {
                _stream = stream ?? Constants.DefaultStream;
            }
            return this;
        }
    }
}