using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for optionally enriching broker parser results using an LLM.
/// </summary>
public interface IBrokerLlmEnrichmentService
{
    /// <summary>
    /// Attempts to enrich the current parse result using the configured LLM layer.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="currentResult">The current parse result.</param>
    /// <param name="settings">The LLM settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The possibly enriched parse result.</returns>
    Task<BrokerParseResult> EnrichAsync(
        ParseContext context,
        BrokerParseResult currentResult,
        BrokerLlmSettings settings,
        CancellationToken cancellationToken = default);
}