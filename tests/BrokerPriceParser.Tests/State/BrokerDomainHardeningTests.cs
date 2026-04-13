using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;
using BrokerPriceParser.Infrastructure.Classification;
using BrokerPriceParser.Infrastructure.Llm;
using BrokerPriceParser.Infrastructure.Normalization;
using BrokerPriceParser.Infrastructure.Parsing;
using BrokerPriceParser.Infrastructure.Scoring;
using BrokerPriceParser.Infrastructure.State;
using BrokerPriceParser.Infrastructure.Validation;
using Xunit;

namespace BrokerPriceParser.Tests.State;

/// <summary>
/// Contains domain-hardening tests for shorthand parsing and market-interest semantics.
/// </summary>
public sealed class BrokerDomainHardeningTests
{
    /// <summary>
    /// Verifies that single-currency broker shorthand can resolve to an implicit XXXUSD pair with shorthand delta.
    /// </summary>
    [Fact]
    public async Task Parse_ShouldResolveImplicitUsdPairAndShorthandDelta_ForBrokerShorthandQuote()
    {
        var harness = CreateHarness();

        var result = await ParseAndApplyAsync(harness, "EUR 1M 10 BFLY 0.9/1.0");

        Assert.Equal(BrokerMessageType.PriceQuote, result.MessageType);
        Assert.Equal(BrokerEventType.QuoteProvided, result.EventType);
        Assert.Equal("EURUSD", result.Instrument.Pair);
        Assert.Equal("1M", result.Instrument.Tenor);
        Assert.Equal("BF", result.Instrument.Structure);
        Assert.Equal(10m, result.Instrument.Delta);
        Assert.Equal(0.9m, result.Quote.Bid);
        Assert.Equal(1.0m, result.Quote.Ask);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that BUYER is treated as market-interest indication rather than execution.
    /// </summary>
    [Fact]
    public async Task Parse_ShouldTreatBuyerAsInterestIndication_NotExecution()
    {
        var harness = CreateHarness();

        var result = await ParseAndApplyAsync(harness, "buyer");

        Assert.Equal(BrokerMessageType.InterestIndication, result.MessageType);
        Assert.Equal(BrokerEventType.MarketInterestIndicated, result.EventType);
        Assert.Equal("BUYER", result.Interest.Side);
        Assert.Equal(string.Empty, result.Action.Verb);

        var state = harness.StateStore.GetOrCreate(harness.ConversationId);
        Assert.Equal("BUYER", state.LatestInterest.Side);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a fully configured parser test harness.
    /// </summary>
    /// <returns>A parser test harness.</returns>
    private static ParserTestHarness CreateHarness()
    {
        var normalizer = new BrokerMessageNormalizer();
        var classifier = new BrokerMessageClassifier();
        var contextResolver = new ConversationContextResolver();
        var validationService = new BrokerValidationService();
        var confidenceScoringService = new ConfidenceScoringService();
        var llmEnrichmentService = new BrokerLlmEnrichmentService(
            new BrokerPromptBuilder(),
            new NullBrokerLlmClient());

        var parseService = new BrokerParseService(
            classifier,
            contextResolver,
            llmEnrichmentService,
            validationService,
            confidenceScoringService,
            new BrokerLlmSettings
            {
                IsEnabled = false,
                UseOnlyForLowConfidence = true,
                LowConfidenceThreshold = 0.55,
                ModelName = "TEST",
                MaxPriorMessages = 5
            });

        var stateStore = new InMemoryConversationStateStore();

        return new ParserTestHarness(
            normalizer,
            parseService,
            stateStore,
            "TEST-CONV");
    }

    // ────────────────────────────────────

    /// <summary>
    /// Parses a message and applies the result to state.
    /// </summary>
    /// <param name="harness">The parser harness.</param>
    /// <param name="rawText">The raw broker text.</param>
    /// <returns>The parsed result.</returns>
    private static async Task<BrokerParseResult> ParseAndApplyAsync(ParserTestHarness harness, string rawText)
    {
        var rawMessage = new RawBrokerMessage
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = harness.ConversationId,
            Source = "UnitTest",
            Broker = "UnitTestBroker",
            RawText = rawText,
            ReceivedUtc = DateTime.UtcNow
        };

        var normalizedMessage = harness.Normalizer.Normalize(rawMessage);
        var state = harness.StateStore.GetOrCreate(harness.ConversationId);

        var context = new ParseContext
        {
            RawMessage = rawMessage,
            NormalizedMessage = normalizedMessage,
            ConversationState = state
        };

        var result = await harness.ParseService.ParseAsync(context);
        harness.StateStore.Apply(harness.ConversationId, result, rawMessage.ReceivedUtc);

        return result;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Represents a configured test harness for parser tests.
    /// </summary>
    private sealed class ParserTestHarness
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParserTestHarness"/> class.
        /// </summary>
        /// <param name="normalizer">The message normalizer.</param>
        /// <param name="parseService">The parse service.</param>
        /// <param name="stateStore">The conversation state store.</param>
        /// <param name="conversationId">The conversation identifier.</param>
        public ParserTestHarness(
            IBrokerMessageNormalizer normalizer,
            IBrokerParseService parseService,
            IConversationStateStore stateStore,
            string conversationId)
        {
            Normalizer = normalizer;
            ParseService = parseService;
            StateStore = stateStore;
            ConversationId = conversationId;
        }

        /// <summary>
        /// Gets the message normalizer.
        /// </summary>
        public IBrokerMessageNormalizer Normalizer { get; }

        /// <summary>
        /// Gets the parse service.
        /// </summary>
        public IBrokerParseService ParseService { get; }

        /// <summary>
        /// Gets the state store.
        /// </summary>
        public IConversationStateStore StateStore { get; }

        /// <summary>
        /// Gets the conversation identifier.
        /// </summary>
        public string ConversationId { get; }
    }
}