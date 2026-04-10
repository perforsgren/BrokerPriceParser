using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Infrastructure.Llm;

/// <summary>
/// Provides optional LLM enrichment for broker parse results.
/// </summary>
public sealed class BrokerLlmEnrichmentService : IBrokerLlmEnrichmentService
{
    private readonly IBrokerPromptBuilder _promptBuilder;
    private readonly IBrokerLlmClient _llmClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrokerLlmEnrichmentService"/> class.
    /// </summary>
    /// <param name="promptBuilder">The prompt builder.</param>
    /// <param name="llmClient">The LLM client.</param>
    public BrokerLlmEnrichmentService(
        IBrokerPromptBuilder promptBuilder,
        IBrokerLlmClient llmClient)
    {
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
    }

    /// <summary>
    /// Attempts to enrich the current parse result using the configured LLM layer.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="currentResult">The current parse result.</param>
    /// <param name="settings">The LLM settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The possibly enriched parse result.</returns>
    public async Task<BrokerParseResult> EnrichAsync(
        ParseContext context,
        BrokerParseResult currentResult,
        BrokerLlmSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(currentResult);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsEnabled)
        {
            return currentResult;
        }

        if (settings.UseOnlyForLowConfidence && currentResult.Quality.Confidence >= settings.LowConfidenceThreshold)
        {
            return currentResult;
        }

        var request = _promptBuilder.Build(context, currentResult, settings);
        var response = await _llmClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccess || !response.IsEnrichmentApplied)
        {
            return currentResult;
        }

        return currentResult;
    }
}