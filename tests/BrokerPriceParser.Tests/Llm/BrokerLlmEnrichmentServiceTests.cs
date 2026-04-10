using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Enums;
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
            new FakeBrokerLlmClient(CreateSuccessfulResponse(CreateStructuredPayload())));

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
            new FakeBrokerLlmClient(CreateSuccessfulResponse(CreateStructuredPayload())));

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
        Assert.Equal(BrokerMessageType.Unknown, enriched.MessageType);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that missing fields are filled from a valid structured payload.
    /// </summary>
    [Fact]
    public async Task EnrichAsync_ShouldFillMissingFields_FromStructuredPayload()
    {
        var service = new BrokerLlmEnrichmentService(
            new BrokerPromptBuilder(),
            new FakeBrokerLlmClient(CreateSuccessfulResponse(CreateStructuredPayload())));

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
        Assert.Equal(BrokerMessageType.PriceQuote, enriched.MessageType);
        Assert.Equal(BrokerEventType.QuoteProvided, enriched.EventType);
        Assert.Equal("NOKSEK", enriched.Instrument.Pair);
        Assert.Equal("1Y", enriched.Instrument.Tenor);
        Assert.Equal("RR", enriched.Instrument.Structure);
        Assert.Equal(25m, enriched.Instrument.Delta);
        Assert.Equal(0.10m, enriched.Quote.Bid);
        Assert.Equal(0.30m, enriched.Quote.Ask);
        Assert.Equal(QuoteStyle.TwoWay, enriched.Quote.QuoteStyle);
        Assert.True(enriched.Quote.IsFirm);
        Assert.Equal(FieldSourceType.Derived, enriched.Provenance.PairSource);
        Assert.Equal(FieldSourceType.Derived, enriched.Provenance.PriceSource);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that explicit fields are not overwritten by the LLM payload.
    /// </summary>
    [Fact]
    public async Task EnrichAsync_ShouldNotOverwriteExistingFields()
    {
        var service = new BrokerLlmEnrichmentService(
            new BrokerPromptBuilder(),
            new FakeBrokerLlmClient(CreateSuccessfulResponse(CreateConflictingPayload())));

        var context = CreateContext();

        var result = new BrokerParseResult
        {
            MessageId = "MSG-001",
            MessageType = BrokerMessageType.PriceQuote,
            EventType = BrokerEventType.QuoteProvided,
            RawMessage = "0.10/0.30",
            NormalizedMessage = "0.10/0.30",
            Instrument = new BrokerInstrument
            {
                Pair = "NOKSEK",
                Tenor = "1Y"
            },
            Quote = new BrokerQuote
            {
                Ask = 0.30m
            },
            Provenance = new FieldProvenance
            {
                PairSource = FieldSourceType.Explicit,
                PriceSource = FieldSourceType.Explicit
            },
            Quality = new BrokerPriceParser.Core.Validation.BrokerParseQuality
            {
                Confidence = 0.10
            }
        };

        var settings = new BrokerLlmSettings
        {
            IsEnabled = true,
            UseOnlyForLowConfidence = true,
            LowConfidenceThreshold = 0.55
        };

        var enriched = await service.EnrichAsync(context, result, settings);

        Assert.Equal("NOKSEK", enriched.Instrument.Pair);
        Assert.Equal("1Y", enriched.Instrument.Tenor);
        Assert.Equal(0.30m, enriched.Quote.Ask);
        Assert.Equal(FieldSourceType.Explicit, enriched.Provenance.PairSource);
        Assert.Equal(FieldSourceType.Explicit, enriched.Provenance.PriceSource);
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
                RawText = "mom int",
                ReceivedUtc = DateTime.UtcNow
            },
            NormalizedMessage = new NormalizedBrokerMessage
            {
                NormalizedText = "MOM INT"
            },
            ConversationState = new ConversationState
            {
                ConversationId = "CONV-001"
            }
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a minimal low-confidence parse result.
    /// </summary>
    /// <param name="confidence">The desired confidence value.</param>
    /// <returns>A broker parse result.</returns>
    private static BrokerParseResult CreateResult(double confidence)
    {
        return new BrokerParseResult
        {
            MessageId = "MSG-001",
            MessageType = BrokerMessageType.Unknown,
            EventType = BrokerEventType.None,
            RawMessage = "mom int",
            NormalizedMessage = "MOM INT",
            Quality = new BrokerPriceParser.Core.Validation.BrokerParseQuality
            {
                Confidence = confidence
            }
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a successful fake LLM response.
    /// </summary>
    /// <param name="payload">The structured JSON payload.</param>
    /// <returns>A broker LLM response.</returns>
    private static BrokerLlmResponse CreateSuccessfulResponse(string payload)
    {
        return new BrokerLlmResponse
        {
            IsSuccess = true,
            IsEnrichmentApplied = true,
            ParsedJsonPayload = payload,
            RawResponseText = payload,
            ErrorMessage = string.Empty
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a structured payload that fills missing fields.
    /// </summary>
    /// <returns>A structured payload JSON string.</returns>
    private static string CreateStructuredPayload()
    {
        return """
        {
          "messageType": "PriceQuote",
          "eventType": "QuoteProvided",
          "instrument": {
            "pair": "NOKSEK",
            "tenor": "1Y",
            "expiry": "",
            "structure": "RR",
            "delta": "25",
            "strikeType": "",
            "strike": "",
            "optionSideBias": ""
          },
          "quote": {
            "bid": "0.10",
            "ask": "0.30",
            "mid": "",
            "quoteStyle": "TwoWay",
            "isFirm": "true"
          },
          "action": {
            "verb": "",
            "side": "",
            "target": "",
            "linkedToPriorQuote": ""
          },
          "llmHints": {
            "confidence": "0.72",
            "notes": ["filled missing values"]
          }
        }
        """;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a structured payload with conflicting values used to verify safe merge behavior.
    /// </summary>
    /// <returns>A structured payload JSON string.</returns>
    private static string CreateConflictingPayload()
    {
        return """
        {
          "messageType": "PriceQuote",
          "eventType": "QuoteProvided",
          "instrument": {
            "pair": "USDJPY",
            "tenor": "6M",
            "expiry": "",
            "structure": "BF",
            "delta": "10",
            "strikeType": "",
            "strike": "",
            "optionSideBias": ""
          },
          "quote": {
            "bid": "0.01",
            "ask": "0.99",
            "mid": "",
            "quoteStyle": "TwoWay",
            "isFirm": "false"
          },
          "action": {
            "verb": "",
            "side": "",
            "target": "",
            "linkedToPriorQuote": ""
          },
          "llmHints": {
            "confidence": "0.20",
            "notes": ["conflicting values"]
          }
        }
        """;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Provides a fake LLM client for deterministic tests.
    /// </summary>
    private sealed class FakeBrokerLlmClient : IBrokerLlmClient
    {
        private readonly BrokerLlmResponse _response;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeBrokerLlmClient"/> class.
        /// </summary>
        /// <param name="response">The response to return.</param>
        public FakeBrokerLlmClient(BrokerLlmResponse response)
        {
            _response = response;
        }

        /// <summary>
        /// Sends a broker LLM request and returns a fake response.
        /// </summary>
        /// <param name="request">The broker LLM request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The fake response.</returns>
        public Task<BrokerLlmResponse> ExecuteAsync(BrokerLlmRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Task.FromResult(_response);
        }
    }
}