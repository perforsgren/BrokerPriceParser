using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Infrastructure.Classification;
using BrokerPriceParser.Infrastructure.Normalization;
using Xunit;

namespace BrokerPriceParser.Tests.Classification;

/// <summary>
/// Contains additional quote-language classification tests for <see cref="BrokerMessageClassifier"/>.
/// </summary>
public sealed class BrokerMessageClassifierV2Tests
{
    /// <summary>
    /// Verifies that suffix one-way quote language is classified correctly.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnPriceQuote_ForSuffixBidQuote()
    {
        var classifier = new BrokerMessageClassifier();
        var message = Normalize("0.15 bid");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.PriceQuote, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that paying language is classified as a quote.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnPriceQuote_ForPayingQuote()
    {
        var classifier = new BrokerMessageClassifier();
        var message = Normalize("paying 0.12");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.PriceQuote, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that offering language is classified as a quote.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnPriceQuote_ForOfferingQuote()
    {
        var classifier = new BrokerMessageClassifier();
        var message = Normalize("offering 0.25");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.PriceQuote, result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that paid language is still classified as an action.
    /// </summary>
    [Fact]
    public void Classify_ShouldReturnActionIntent_ForPaidAction()
    {
        var classifier = new BrokerMessageClassifier();
        var message = Normalize("paid 0.30");

        var result = classifier.Classify(message);

        Assert.Equal(BrokerMessageType.ActionIntent, result);
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