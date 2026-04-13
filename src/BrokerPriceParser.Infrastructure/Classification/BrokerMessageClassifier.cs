using System.Text.RegularExpressions;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Infrastructure.Classification;

/// <summary>
/// Provides rule-based classification for normalized broker messages.
/// </summary>
public sealed class BrokerMessageClassifier : IBrokerMessageClassifier
{
    private static readonly HashSet<string> NoiseMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        "OK",
        "K",
        "KK",
        "THANKS",
        "THX",
        "TY",
        "HI",
        "HELLO",
        "YO"
    };

    /// <summary>
    /// Classifies a normalized broker message.
    /// </summary>
    /// <param name="message">The normalized message to classify.</param>
    /// <returns>The resolved broker message type.</returns>
    public BrokerMessageType Classify(NormalizedBrokerMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var text = message.NormalizedText;

        if (string.IsNullOrWhiteSpace(text))
        {
            return BrokerMessageType.Noise;
        }

        if (LooksLikeAction(text))
        {
            return BrokerMessageType.ActionIntent;
        }

        if (LooksLikePriceUpdate(text))
        {
            return BrokerMessageType.PriceUpdate;
        }

        if (LooksLikePriceQuote(text))
        {
            return BrokerMessageType.PriceQuote;
        }

        if (LooksLikeInstrumentRequest(text, message))
        {
            return BrokerMessageType.InstrumentRequest;
        }

        if (LooksLikeClarification(text))
        {
            return BrokerMessageType.Clarification;
        }

        if (LooksLikeNoise(text))
        {
            return BrokerMessageType.Noise;
        }

        return BrokerMessageType.Unknown;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects common execution and dealing action language.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text resembles an action message; otherwise <c>false</c>.</returns>
    private static bool LooksLikeAction(string text)
    {
        return Regex.IsMatch(
            text,
            @"\b(TAKE|TAK|MINE|LIFT|HIT|PAID|SOLD|YOURS|DONE)\b");
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects quote revision and update language such as FLAT BID or BETTER 0.10/0.30.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text resembles a price update; otherwise <c>false</c>.</returns>
    private static bool LooksLikePriceUpdate(string text)
    {
        if (Regex.IsMatch(text, @"\bFLAT\s+(BID|OFFER|OFR|ASK|MID)\b"))
        {
            return true;
        }

        var hasUpdateWord = Regex.IsMatch(
            text,
            @"\b(BETTER|WORSE|WIDER|TIGHTER|HIGHER|LOWER|REVISED|REVISE|UPDATE|UPDATED|IMPROVED|IMPROVE)\b");

        var hasQuoteContext =
            LooksLikeTwoWayQuote(text)
            || LooksLikeOneWayQuote(text)
            || Regex.IsMatch(text, @"\b(BID|OFFER|OFR|ASK|MID)\b")
            || Regex.IsMatch(text, @"-?\d+(\.\d+)?");

        return hasUpdateWord && hasQuoteContext;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects quote-like messages.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text resembles a quote; otherwise <c>false</c>.</returns>
    private static bool LooksLikePriceQuote(string text)
    {
        return LooksLikeTwoWayQuote(text) || LooksLikeOneWayQuote(text);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects a simple two-way price format such as 0.10/0.30.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text looks like a two-way quote; otherwise <c>false</c>.</returns>
    private static bool LooksLikeTwoWayQuote(string text)
    {
        return Regex.IsMatch(text, @"(?<!\d)-?\d+(\.\d+)?\s*/\s*-?\d+(\.\d+)?");
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects one-way quote patterns such as BID 0.15, 0.15 BID, PAYING 0.12 or OFFERING 0.25.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text looks like a one-way quote; otherwise <c>false</c>.</returns>
    private static bool LooksLikeOneWayQuote(string text)
    {
        var prefixMatch = Regex.IsMatch(
            text,
            @"\b(BID|ASK|OFFER|OFR|MID|PAYING|OFFERING)\b\s*(-?\d+(\.\d+)?)");

        var suffixMatch = Regex.IsMatch(
            text,
            @"(-?\d+(\.\d+)?)\s*\b(BID|ASK|OFFER|OFR|MID)\b");

        return prefixMatch || suffixMatch;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects an instrument or market request pattern.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <param name="message">The normalized message metadata.</param>
    /// <returns><c>true</c> if the text resembles an instrument request; otherwise <c>false</c>.</returns>
    private static bool LooksLikeInstrumentRequest(string text, NormalizedBrokerMessage message)
    {
        var containsRequestLanguage = Regex.IsMatch(
            text,
            @"\b(MKT|MARKET|PLS|PLEASE|WHERE|SHOW|PRICE|PRICING|ANY|RUN|LOOK)\b");

        var hasPair = !string.IsNullOrWhiteSpace(message.DetectedCurrencyPair);
        var hasTenor = !string.IsNullOrWhiteSpace(message.DetectedTenor);
        var hasStructure = !string.IsNullOrWhiteSpace(message.DetectedStructure);
        var hasDelta = message.DetectedDelta.HasValue;

        if (hasPair && hasTenor)
        {
            return true;
        }

        if (hasPair && hasStructure)
        {
            return true;
        }

        if (hasPair && containsRequestLanguage)
        {
            return true;
        }

        if (hasTenor && hasStructure && containsRequestLanguage)
        {
            return true;
        }

        if (hasPair && hasTenor && hasDelta)
        {
            return true;
        }

        return false;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects clarification, follow-up or directional-interest messages such as WHAT ABOUT 2Y, SAME IN 3M, BUYER or SELLER.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text resembles a clarification or market-color message; otherwise <c>false</c>.</returns>
    private static bool LooksLikeClarification(string text)
    {
        if (Regex.IsMatch(text, @"\b(WHAT ABOUT|HOW ABOUT)\b"))
        {
            return true;
        }

        if (Regex.IsMatch(text, @"\bSAME\b"))
        {
            return true;
        }

        if (Regex.IsMatch(text, @"\bIN\s+\d+(D|W|M|Y)\b"))
        {
            return true;
        }

        if (Regex.IsMatch(text, @"\b(BUYER|SELLER)\b"))
        {
            return true;
        }

        return false;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects conversational noise or low-information acknowledgements.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text resembles noise; otherwise <c>false</c>.</returns>
    private static bool LooksLikeNoise(string text)
    {
        return NoiseMessages.Contains(text);
    }
}