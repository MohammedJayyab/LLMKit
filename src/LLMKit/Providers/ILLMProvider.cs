using LLMKit.Models;

namespace LLMKit.Providers;

/// <summary>
/// Defines the contract for Large Language Model providers.
/// This interface ensures that all LLM providers implement the necessary functionality
/// for generating text responses from prompts.
/// </summary>
public interface ILLMProvider : IDisposable
{
    /// <summary>
    /// Gets the endpoint URI for the provider's API.
    /// </summary>
    Uri Endpoint { get; }

    /// <summary>
    /// Generates text based on the provided request parameters.
    /// </summary>
    /// <param name="request">The request containing messages and generation parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A response containing the generated text.</returns>
    /// <exception cref="Exceptions.LLMException">Thrown when an error occurs during text generation.</exception>
    Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets what the provider does for its model with the settings of the given request.
    /// Providers that do not override it report no capabilities.
    /// </summary>
    /// <param name="request">The request settings (reasoning effort, max tokens); messages are not used.</param>
    /// <returns>The capabilities.</returns>
    ProviderCapabilities GetCapabilities(LLMRequest request) => new();
}