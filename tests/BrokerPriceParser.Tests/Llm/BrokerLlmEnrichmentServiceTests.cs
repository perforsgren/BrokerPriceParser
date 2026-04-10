using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;
using BrokerPriceParser.Infrastructure.Llm;
using Xunit;

namespace BrokerPriceParser.Tests.Llm;

/// <summary>
/// Contains unit tests for <see cref="BrokerLlmEnrichmentService"/>.
/// </summary>
public sealed class BrokerLlmEnrichmentServiceTests
{
    /// <summary>
    /// Verifies that enrichment is skipped when disabled.
    /// </summary>
    [Fact]
    public async Task EnrichAsync_ShouldReturnOriginalResult_WhenLlmIsDisabled()
    {
        var service = new BrokerLlmEnrichmentService(
            new BrokerPromptBuilder(),
            new NullBrokerLlmClient());

        var context = CreateContext();
        var result = CreateResult(confidence: 0.10);

        var settings = new BrokerLlmSettings
        {
            IsEnabled = false
        };

        var enriched = await service.EnrichAsync(context, result, settings);

        Assert.Same(result, enriched);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that enrichment is skipped for high-confidence results when configured that way.
    /// </summary>
    [Fact]
    public async Task EnrichAsync_ShouldReturnOriginalResult_WhenConfidenceIsAboveThreshold()
    {
        var service = new BrokerLlmEnrichmentService(
            new BrokerPromptBuilder(),
            new NullBrokerLlmClient());

        var context = CreateContext();
        var result = CreateResult(confidence: 0.90);

        var settings = new BrokerLlmSettings
        {
            IsEnabled = true,
            UseOnlyForLowConfidence = true,
            LowConfidenceThreshold = 0.55
        };

        var enriched = await service.EnrichAsync(context, result, settings);

        Assert.Same(result, enriched);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that a no-op LLM client does not alter the result.
    /// </summary>
    [Fact]
    public async Task EnrichAsync_ShouldReturnOriginalResult_WhenNoProviderIsConfigured()
    {
        var service = new BrokerLlmEnrichmentService(
            new BrokerPromptBuilder(),
            new NullBrokerLlmClient());

        var context = CreateContext();
        var result = CreateResult(confidence: 0.10);

        var settings = new BrokerLlmSettings
        {
            IsEnabled = true,
            UseOnlyForLowConfidence = true,
            LowConfidenceThreshold = 0.55
        };

        var enriched = await service.EnrichAsync(context, result, settings);

        Assert.Same(result, enriched);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a test parse context.
    /// </summary>
    /// <returns>A parse context.</returns>
    private static ParseContext CreateContext()
    {
        return new ParseContext
        {
            RawMessage = new RawBrokerMessage
            {
                MessageId = "MSG-001",
                ConversationId = "CONV-001",
                RawText = "ok take",
                ReceivedUtc = DateTime.UtcNow
            },
            NormalizedMessage = new NormalizedBrokerMessage
            {
                NormalizedText = "OK TAKE"
            },
            ConversationState = new ConversationState
            {
                ConversationId = "CONV-001"
            }
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a test broker parse result.
    /// </summary>
    /// <param name="confidence">The desired confidence value.</param>
    /// <returns>A broker parse result.</returns>
    private static BrokerParseResult CreateResult(double confidence)
    {
        return new BrokerParseResult
        {
            MessageId = "MSG-001",
            RawMessage = "ok take",
            NormalizedMessage = "OK TAKE",
            Quality = new BrokerPriceParser.Core.Validation.BrokerParseQuality
            {
                Confidence = confidence
            }
        };
    }
}