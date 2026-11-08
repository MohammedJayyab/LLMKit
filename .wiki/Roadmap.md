# 🗺️ LLMKit Roadmap

This document outlines potential future improvements for LLMKit.

## 🎯 Current Version (4.0.0)
 
Current features include:
- ✅ Unified interface for multiple LLM providers
- ✅ Thread-safe implementation
- ✅ Conversation management
- ✅ Fluent API for message building
- ✅ Configurable parameters (tokens, temperature, etc.)
- ✅ Comprehensive error handling
- ✅ Dependency injection support
- ✅ Cancellation token support
- ✅ Custom endpoint support for all providers
- ✅ Multimodal support (text + images)
- ✅ System message configuration
- ✅ Automatic retries for failed requests (network errors, 408, 429 and 5xx)
- ✅ Providers: OpenAI, Anthropic (Claude), Gemini, DeepSeek and LLM Studio (local models)
- ✅ Reasoning / thinking effort control across providers, with access to the returned reasoning
- ✅ Per-model reasoning parameters: each model gets only the values it accepts (new in 4.0.0)
- ✅ Token usage per response: input, output and reasoning tokens (new in 4.0.0)
- ✅ Finish reason per response: complete, cut off at `maxTokens`, or blocked (new in 4.0.0)
- ✅ Model capabilities: check reasoning effort, temperature, reasoning text and image support (new in 4.0.0)
- ✅ List of the supported providers (new in 4.0.0)
- ✅ `maxTokens` without the 4096 cap: the model's own limit applies (new in 4.0.0)

## 📈 Planned Improvements

Potential enhancements being considered:

### Core Features
- Message streaming support
- Enhanced error handling and logging
- Additional provider support (for example Azure OpenAI, Mistral, Ollama)
- Responses API support for OpenAI (needed for `*-codex` and `*-pro` models)
- `XHigh` and `Max` reasoning effort levels
- Configurable request timeout (long responses with a high `maxTokens` can exceed the fixed timeout)
- Performance optimizations
- Retry policies with configurable strategies
- Memory usage optimizations
- Advanced conversation management features
- Support for function calling

### Developer Experience
- Expanded documentation
- More code examples
- Additional unit tests
- Integration tests
- Performance benchmarks
- Migration guides
- Best practices documentation

## 📢 Feedback & Suggestions

We welcome community input! Please:
- 🐛 [Report issues](https://github.com/MohammedJayyab/LLMKit/issues)
- 💡 [Submit feature requests](https://github.com/MohammedJayyab/LLMKit/issues/new)
- 🤝 [Contribute](https://github.com/MohammedJayyab/LLMKit/wiki/Contributing)
- 💬 Join discussions

## ❓ Questions?

For questions about the roadmap or to discuss specific features:
- 📮 Open a [GitHub Discussion](https://github.com/MohammedJayyab/LLMKit/discussions)
- 📝 Check our [Wiki](https://github.com/MohammedJayyab/LLMKit/wiki)
- 📧 Contact the maintainers

---
*Note: This roadmap represents potential improvements and may change based on community needs and usage patterns.* 