using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Infrastructure.Normalization;

namespace BrokerPriceParser.Tests.Normalization;

/// <summary>
/// Contains unit tests for <see cref="BrokerMessageNormalizer"/>.
/// </summary>
public sealed class BrokerMessageNormalizerTests
{
    /// <summary>
    /// Verifies that a standard RR request is normalized and detected correctly.
    /// </summary>
    [Fact]
    public void Normalize_ShouldDetectPairTenorStructureAndDelta_ForStandardRiskReversalRequest()
    {
        var normalizer = new BrokerMessageNormalizer();

        var message = new RawBrokerMessage
        {
            MessageId = "T1",
            ConversationId = "C1",
            RawText = "NOK/SEK 1Y 25 delta rr pls",
            ReceivedUtc = DateTime.UtcNow
        };

        var result = normalizer.Normalize(message);

        Assert.Equal("NOKSEK 1Y 25D RR PLS", result.NormalizedText);
        Assert.Equal("NOKSEK", result.DetectedCurrencyPair);
        Assert.Equal("1Y", result.DetectedTenor);
        Assert.Equal("RR", result.DetectedStructure);
        Assert.Equal(25m, result.DetectedDelta);
        Assert.Contains("NormalizeCurrencyPair", result.AppliedNormalizationRules);
        Assert.Contains("NormalizeDeltaTokens", result.AppliedNormalizationRules);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that ATMF aliases are normalized correctly.
    /// </summary>
    [Fact]
    public void Normalize_ShouldConvertAtTheMoneyForward_ToAtmf()
    {
        var normalizer = new BrokerMessageNormalizer();

        var message = new RawBrokerMessage
        {
            MessageId = "T2",
            ConversationId = "C1",
            RawText = "EUR SEK 3m at the money forward",
            ReceivedUtc = DateTime.UtcNow
        };

        var result = normalizer.Normalize(message);

        Assert.Equal("EURSEK 3M ATMF", result.NormalizedText);
        Assert.Equal("EURSEK", result.DetectedCurrencyPair);
        Assert.Equal("3M", result.DetectedTenor);
        Assert.Equal("ATMF", result.DetectedStructure);
        Assert.Null(result.DetectedDelta);
        Assert.Contains("NormalizeAtmAliases", result.AppliedNormalizationRules);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that butterfly aliases are normalized correctly.
    /// </summary>
    [Fact]
    public void Normalize_ShouldConvertButterflyAlias_ToBf()
    {
        var normalizer = new BrokerMessageNormalizer();

        var message = new RawBrokerMessage
        {
            MessageId = "T3",
            ConversationId = "C1",
            RawText = "USD JPY 6m butterfly",
            ReceivedUtc = DateTime.UtcNow
        };

        var result = normalizer.Normalize(message);

        Assert.Equal("USDJPY 6M BF", result.NormalizedText);
        Assert.Equal("USDJPY", result.DetectedCurrencyPair);
        Assert.Equal("6M", result.DetectedTenor);
        Assert.Equal("BF", result.DetectedStructure);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that tenor can still be detected when pair is absent.
    /// </summary>
    [Fact]
    public void Normalize_ShouldDetectTenor_WhenPairIsMissing()
    {
        var normalizer = new BrokerMessageNormalizer();

        var message = new RawBrokerMessage
        {
            MessageId = "T4",
            ConversationId = "C1",
            RawText = "what about 2y",
            ReceivedUtc = DateTime.UtcNow
        };

        var result = normalizer.Normalize(message);

        Assert.Equal("WHAT ABOUT 2Y", result.NormalizedText);
        Assert.Equal(string.Empty, result.DetectedCurrencyPair);
        Assert.Equal("2Y", result.DetectedTenor);
        Assert.Equal(string.Empty, result.DetectedStructure);
        Assert.Null(result.DetectedDelta);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that Greek delta symbols are normalized correctly.
    /// </summary>
    [Fact]
    public void Normalize_ShouldConvertGreekDeltaSymbol_ToD()
    {
        var normalizer = new BrokerMessageNormalizer();

        var message = new RawBrokerMessage
        {
            MessageId = "T5",
            ConversationId = "C1",
            RawText = "NOK/SEK 1Y 25Δ risk reversal",
            ReceivedUtc = DateTime.UtcNow
        };

        var result = normalizer.Normalize(message);

        Assert.Equal("NOKSEK 1Y 25D RR", result.NormalizedText);
        Assert.Equal("NOKSEK", result.DetectedCurrencyPair);
        Assert.Equal("1Y", result.DetectedTenor);
        Assert.Equal("RR", result.DetectedStructure);
        Assert.Equal(25m, result.DetectedDelta);
        Assert.Contains("NormalizeGreekCharacters", result.AppliedNormalizationRules);
        Assert.Contains("NormalizeStructureAliases", result.AppliedNormalizationRules);
    }

    /// <summary>
    /// Verifies that a single currency token can conservatively imply an XXXUSD pair in broker shorthand.
    /// </summary>
    [Fact]
    public void Normalize_ShouldDetectImplicitUsdPair_ForSingleCurrencyBrokerShorthand()
    {
        var normalizer = new BrokerMessageNormalizer();

        var message = new RawBrokerMessage
        {
            MessageId = "T6",
            ConversationId = "C1",
            RawText = "EUR 1M BFLY 0.9/1.0",
            ReceivedUtc = DateTime.UtcNow
        };

        var result = normalizer.Normalize(message);

        Assert.Equal("EUR 1M BF 0.9/1.0", result.NormalizedText);
        Assert.Equal("EURUSD", result.DetectedCurrencyPair);
        Assert.Equal("1M", result.DetectedTenor);
        Assert.Equal("BF", result.DetectedStructure);
    }


    /// <summary>
    /// Verifies that broker shorthand can imply an XXXUSD pair and shorthand delta before BF is normalized correctly.
    /// </summary>
    [Fact]
    public void Normalize_ShouldDetectImplicitUsdPairAndShorthandDelta_ForSingleCurrencyBrokerShorthand()
    {
        var normalizer = new BrokerMessageNormalizer();

        var message = new RawBrokerMessage
        {
            MessageId = "T6",
            ConversationId = "C1",
            RawText = "EUR 1M 10 BFLY 0.9/1.0",
            ReceivedUtc = DateTime.UtcNow
        };

        var result = normalizer.Normalize(message);

        Assert.Equal("EUR 1M 10D BF 0.9/1.0", result.NormalizedText);
        Assert.Equal("EURUSD", result.DetectedCurrencyPair);
        Assert.Equal("1M", result.DetectedTenor);
        Assert.Equal("BF", result.DetectedStructure);
        Assert.Equal(10m, result.DetectedDelta);
        Assert.Contains("NormalizeStructureAliases", result.AppliedNormalizationRules);
        Assert.Contains("NormalizeShorthandDeltaBeforeStructure", result.AppliedNormalizationRules);
    }
}