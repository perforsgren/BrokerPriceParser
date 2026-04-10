using BrokerPriceParser.Core.Llm;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for sending broker parser requests to an LLM provider.
/// </summary>
public interface IBrokerLlmClient
{
    /// <summary>
    /// Sends a broker LLM request and returns the provider response.
    /// </summary>
    /// <param name="request">The broker LLM request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The LLM response.</returns>
    Task<BrokerLlmResponse> ExecuteAsync(BrokerLlmRequest request, CancellationToken cancellationToken = default);
}