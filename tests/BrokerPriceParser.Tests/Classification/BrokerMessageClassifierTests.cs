using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Infrastructure.Classification;
using BrokerPriceParser.Infrastructure.Normalization;
using Xunit;

namespace BrokerPriceParser.Tests.Classification;

/// <summary>
/// Contains unit tests for <see cref="BrokerMessageClassifier"/>.
/// </summary>
public sealed class BrokerMessageClassifierTests
{
    /// <summary>
    /// Verifies that a standard market request is classified correctly.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnInstrumentRequest_ForStandardRiskReversalRequest()
    {
        var classifier = CreateClassifier();
        var message = Normalize("NOK/SEK 1Y 25 delta rr pls");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.InstrumentRequest, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that a two-way quote is classified correctly.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnPriceQuote_ForTwoWayQuote()
    {
        var classifier = CreateClassifier();
        var message = Normalize("0.10/0.30");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.PriceQuote, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that a one-way quote is classified correctly.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnPriceQuote_ForOneWayBidQuote()
    {
        var classifier = CreateClassifier();
        var message = Normalize("bid 0.15");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.PriceQuote, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that flat bid language is classified as a price update.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnPriceUpdate_ForFlatBid()
    {
        var classifier = CreateClassifier();
        var message = Normalize("flat bid");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.PriceUpdate, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that explicit quote improvement language is classified as a price update.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnPriceUpdate_ForBetterTwoWayQuote()
    {
        var classifier = CreateClassifier();
        var message = Normalize("better 0.10/0.30");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.PriceUpdate, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that take language is classified as an action.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnActionIntent_ForTake()
    {
        var classifier = CreateClassifier();
        var message = Normalize("ok take");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.ActionIntent, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that mine language is classified as an action.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnActionIntent_ForMine()
    {
        var classifier = CreateClassifier();
        var message = Normalize("mine");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.ActionIntent, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that what-about follow-up language is classified as clarification.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnClarification_ForWhatAboutFollowUp()
    {
        var classifier = CreateClassifier();
        var message = Normalize("what about 2y");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.Clarification, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that same-in-tenor follow-up language is classified as clarification.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnClarification_ForSameInTenor()
    {
        var classifier = CreateClassifier();
        var message = Normalize("same in 3m");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.Clarification, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that low-information acknowledgements are classified as noise.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnNoise_ForThanks()
    {
        var classifier = CreateClassifier();
        var message = Normalize("thanks");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.Noise, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that unrecognized text remains unknown.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnUnknown_ForUnrecognizedText()
    {
        var classifier = CreateClassifier();
        var message = Normalize("MOM INT");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.Unknown, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a classifier instance for tests.
    /// </summary>
    /// <returns>A broker message classifier.</returns>
    private static BrokerMessageClassifier CreateClassifier()
    {
        return new BrokerMessageClassifier();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes raw text into a normalized broker message for classification tests.
    /// </summary>
    /// <param name="rawText">The raw text.</param>
    /// <returns>A normalized broker message.</returns>
    private static NormalizedBrokerMessage Normalize(string rawText)
    {
        var normalizer = new BrokerMessageNormalizer();

        var rawMessage = new RawBrokerMessage
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = "TEST-CONV",
            Source = "UnitTest",
            Broker = "UnitTestBroker",
            RawText = rawText,
            ReceivedUtc = DateTime.UtcNow
        };

        return normalizer.Normalize(rawMessage);
    }
}