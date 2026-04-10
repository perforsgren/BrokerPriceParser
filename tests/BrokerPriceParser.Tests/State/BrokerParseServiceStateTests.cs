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
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Infrastructure.Llm;
using Xunit;

namespace BrokerPriceParser.Tests.State;

/// <summary>
/// Contains end-to-end stateful tests for broker parsing.
/// </summary>
public sealed class BrokerParseServiceStateTests
{
    /// <summary>
    /// Verifies that a quote inherits instrument context from a prior request.
    /// </summary>
    [Fact]
    public async Task ParseSequence_ShouldInheritInstrumentIntoQuote()
    {
        var harness = CreateHarness();

        var firstResult = await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        var secondResult = await ParseAndApplyAsync(harness, "0.10/0.30");

        Assert.Equal(BrokerMessageType.InstrumentRequest, firstResult.MessageType);
        Assert.Equal(BrokerMessageType.PriceQuote, secondResult.MessageType);
        Assert.Equal("NOKSEK", secondResult.Instrument.Pair);
        Assert.Equal("1Y", secondResult.Instrument.Tenor);
        Assert.Equal("RR", secondResult.Instrument.Structure);
        Assert.Equal(25m, secondResult.Instrument.Delta);
        Assert.Equal(0.10m, secondResult.Quote.Bid);
        Assert.Equal(0.30m, secondResult.Quote.Ask);
        Assert.True(secondResult.ContextUsage.UsedContext);
        Assert.Contains("Instrument.Pair", secondResult.ContextUsage.ResolvedFromContext);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that FLAT BID resolves from the prior quote in state.
    /// </summary>
    [Fact]
    public async Task ParseSequence_ShouldResolveFlatBidFromPriorQuote()
    {
        var harness = CreateHarness();

        await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        await ParseAndApplyAsync(harness, "0.10/0.30");
        var result = await ParseAndApplyAsync(harness, "flat bid");

        Assert.Equal(BrokerMessageType.PriceUpdate, result.MessageType);
        Assert.Equal(BrokerEventType.QuoteRevised, result.EventType);
        Assert.Equal(0.10m, result.Quote.Bid);
        Assert.Equal(QuoteStyle.BidOnly, result.Quote.QuoteStyle);
        Assert.Equal(FieldSourceType.InferredFromContext, result.Provenance.PriceSource);
        Assert.Contains("Quote.Bid", result.ContextUsage.ResolvedFromContext);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that TAKE resolves as a lift of the prior offer.
    /// </summary>
    [Fact]
    public async Task ParseSequence_ShouldResolveTakeAsLiftOfferLinkedToPriorQuote()
    {
        var harness = CreateHarness();

        await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        await ParseAndApplyAsync(harness, "0.10/0.30");
        var result = await ParseAndApplyAsync(harness, "ok take");

        Assert.Equal(BrokerMessageType.ActionIntent, result.MessageType);
        Assert.Equal(BrokerEventType.LiftOffer, result.EventType);
        Assert.Equal("TAKE", result.Action.Verb);
        Assert.Equal("ASK", result.Action.Side);
        Assert.True(result.Action.LinkedToPriorQuote);
        Assert.Equal("NOKSEK", result.Instrument.Pair);
        Assert.Equal("1Y", result.Instrument.Tenor);
        Assert.Contains("Action.LinkedToPriorQuote", result.ContextUsage.ResolvedFromContext);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that SAME IN 2Y inherits the active instrument while overriding tenor.
    /// </summary>
    [Fact]
    public async Task ParseSequence_ShouldResolveSameInTenorByInheritingState()
    {
        var harness = CreateHarness();

        await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        var result = await ParseAndApplyAsync(harness, "same in 2y");

        Assert.Equal(BrokerMessageType.Clarification, result.MessageType);
        Assert.Equal(BrokerEventType.ClarificationRequested, result.EventType);
        Assert.Equal("NOKSEK", result.Instrument.Pair);
        Assert.Equal("2Y", result.Instrument.Tenor);
        Assert.Equal("RR", result.Instrument.Structure);
        Assert.Equal(25m, result.Instrument.Delta);

        var state = harness.StateStore.GetOrCreate(harness.ConversationId);
        Assert.Equal("2Y", state.CurrentInstrument.Tenor);
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