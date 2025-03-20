using LLMKit.Models;

namespace LLMKit.Providers;

/// <summary>
/// Defines the contract for Large Language Model providers.
/// This interface ensures that all LLM providers implement the necessary functionality
/// for generating text responses from prompts.
/// </summary>
public interface ILLMProvider
{
    Uri Endpoint { get; }

    /// <summary>
    /// Generates text based on the provided request parameters.
    /// </summary>
    /// <param name="request">The request containing messages and generation parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A response containing the generated text.</returns>
    /// <exception cref="LLMOrchestratorException">Thrown when an error occurs during text generation.</exception>
    Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default);
}