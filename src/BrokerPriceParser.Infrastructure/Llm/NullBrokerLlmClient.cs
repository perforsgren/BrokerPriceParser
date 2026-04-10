using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Llm;

namespace BrokerPriceParser.Infrastructure.Llm;

/// <summary>
/// Provides a no-op LLM client used until a real provider integration is added.
/// </summary>
public sealed class NullBrokerLlmClient : IBrokerLlmClient
{
    /// <summary>
    /// Sends a broker LLM request and returns a no-op response.
    /// </summary>
    /// <param name="request">The broker LLM request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A no-op LLM response.</returns>
    public Task<BrokerLlmResponse> ExecuteAsync(BrokerLlmRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = new BrokerLlmResponse
        {
            IsSuccess = false,
            IsEnrichmentApplied = false,
            RawResponseText = string.Empty,
            ParsedJsonPayload = string.Empty,
            ErrorMessage = "No LLM provider is configured."
        };

        return Task.FromResult(response);
    }
}