using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;
using BrokerPriceParser.Infrastructure.Classification;
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
/// Contains stateful quote and action extraction tests.
/// </summary>
public sealed class BrokerQuoteAndActionExtractionTests
{
    /// <summary>
    /// Verifies that a firm two-way quote is extracted correctly.
    /// </summary>
    [Fact]
    public async Task Parse_ShouldExtractFirmTwoWayQuote()
    {
        var harness = CreateHarness();

        await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        var result = await ParseAndApplyAsync(harness, "showing 0.10/0.30 good");

        Assert.Equal(BrokerMessageType.PriceQuote, result.MessageType);
        Assert.Equal(BrokerEventType.QuoteProvided, result.EventType);
        Assert.Equal(0.10m, result.Quote.Bid);
        Assert.Equal(0.30m, result.Quote.Ask);
        Assert.Equal(QuoteStyle.TwoWay, result.Quote.QuoteStyle);
        Assert.True(result.Quote.IsFirm);
        Assert.Equal(FieldSourceType.Explicit, result.Provenance.PriceSource);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that paying language is extracted as a bid quote.
    /// </summary>
    [Fact]
    public async Task Parse_ShouldExtractPayingAsBidQuote()
    {
        var harness = CreateHarness();

        await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        var result = await ParseAndApplyAsync(harness, "paying 0.12");

        Assert.Equal(BrokerMessageType.PriceQuote, result.MessageType);
        Assert.Equal(0.12m, result.Quote.Bid);
        Assert.Equal(QuoteStyle.BidOnly, result.Quote.QuoteStyle);
        Assert.Equal("NOKSEK", result.Instrument.Pair);
        Assert.Equal("1Y", result.Instrument.Tenor);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that suffix offer language is extracted as an ask quote.
    /// </summary>
    [Fact]
    public async Task Parse_ShouldExtractSuffixOfferAsAskQuote()
    {
        var harness = CreateHarness();

        await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        var result = await ParseAndApplyAsync(harness, "0.27 offer");

        Assert.Equal(BrokerMessageType.PriceQuote, result.MessageType);
        Assert.Equal(0.27m, result.Quote.Ask);
        Assert.Equal(QuoteStyle.AskOnly, result.Quote.QuoteStyle);
        Assert.Equal("NOKSEK", result.Instrument.Pair);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that PAID with explicit price resolves as an ask-side action with explicit price target.
    /// </summary>
    [Fact]
    public async Task Parse_ShouldResolvePaidWithExplicitPrice()
    {
        var harness = CreateHarness();

        await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        var result = await ParseAndApplyAsync(harness, "paid 0.30");

        Assert.Equal(BrokerMessageType.ActionIntent, result.MessageType);
        Assert.Equal(BrokerEventType.LiftOffer, result.EventType);
        Assert.Equal("PAID", result.Action.Verb);
        Assert.Equal("ASK", result.Action.Side);
        Assert.False(result.Action.LinkedToPriorQuote);
        Assert.Equal("0.3", result.Action.Target);
        Assert.Equal(0.30m, result.Quote.Ask);
        Assert.Equal(QuoteStyle.AskOnly, result.Quote.QuoteStyle);
        Assert.Equal(FieldSourceType.Explicit, result.Provenance.PriceSource);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that SOLD with explicit price resolves as a bid-side action with explicit price target.
    /// </summary>
    [Fact]
    public async Task Parse_ShouldResolveSoldWithExplicitPrice()
    {
        var harness = CreateHarness();

        await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        var result = await ParseAndApplyAsync(harness, "sold 0.10");

        Assert.Equal(BrokerMessageType.ActionIntent, result.MessageType);
        Assert.Equal(BrokerEventType.HitBid, result.EventType);
        Assert.Equal("SOLD", result.Action.Verb);
        Assert.Equal("BID", result.Action.Side);
        Assert.False(result.Action.LinkedToPriorQuote);
        Assert.Equal("0.1", result.Action.Target);
        Assert.Equal(0.10m, result.Quote.Bid);
        Assert.Equal(QuoteStyle.BidOnly, result.Quote.QuoteStyle);
        Assert.Equal(FieldSourceType.Explicit, result.Provenance.PriceSource);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that TAKE without explicit price links to the latest ask.
    /// </summary>
    [Fact]
    public async Task Parse_ShouldResolveTakeAgainstLatestAsk()
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
        Assert.Equal("LATEST_ASK", result.Action.Target);
        Assert.Equal(0.30m, result.Quote.Ask);
        Assert.Equal(FieldSourceType.InferredFromContext, result.Provenance.PriceSource);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that HIT without explicit price links to the latest bid.
    /// </summary>
    [Fact]
    public async Task Parse_ShouldResolveHitAgainstLatestBid()
    {
        var harness = CreateHarness();

        await ParseAndApplyAsync(harness, "NOK/SEK 1Y 25 delta rr pls");
        await ParseAndApplyAsync(harness, "0.10/0.30");
        var result = await ParseAndApplyAsync(harness, "ok hit");

        Assert.Equal(BrokerMessageType.ActionIntent, result.MessageType);
        Assert.Equal(BrokerEventType.HitBid, result.EventType);
        Assert.Equal("HIT", result.Action.Verb);
        Assert.Equal("BID", result.Action.Side);
        Assert.True(result.Action.LinkedToPriorQuote);
        Assert.Equal("LATEST_BID", result.Action.Target);
        Assert.Equal(0.10m, result.Quote.Bid);
        Assert.Equal(FieldSourceType.InferredFromContext, result.Provenance.PriceSource);
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