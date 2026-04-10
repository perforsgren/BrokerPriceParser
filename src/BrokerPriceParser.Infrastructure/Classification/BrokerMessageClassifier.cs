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

        if (LooksLikeTwoWayQuote(text) || LooksLikeOneWayQuote(text))
        {
            return BrokerMessageType.PriceQuote;
        }

        if (LooksLikeInstrumentRequest(text))
        {
            return BrokerMessageType.InstrumentRequest;
        }

        if (LooksLikeClarification(text))
        {
            return BrokerMessageType.Clarification;
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
        return Regex.IsMatch(text, @"\b(TAKE|TAK|MINE|LIFT|HIT|PAID|SOLD|YOURS|BUYER|SELLER)\b");
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
    /// Detects a one-way quote combined with bid, offer or mid language.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text looks like a one-way quote; otherwise <c>false</c>.</returns>
    private static bool LooksLikeOneWayQuote(string text)
    {
        return Regex.IsMatch(text, @"\b(BID|OFFER|OFR|ASK|MID)\b")
            && Regex.IsMatch(text, @"-?\d+(\.\d+)?");
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects an instrument or market request pattern.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text resembles an instrument request; otherwise <c>false</c>.</returns>
    private static bool LooksLikeInstrumentRequest(string text)
    {
        var containsPair = Regex.IsMatch(text, @"\b[A-Z]{6}\b");
        var containsTenor = Regex.IsMatch(text, @"\b\d+(D|W|M|Y)\b");
        var containsStructure = Regex.IsMatch(text, @"\b(RR|BF|ATM|ATMF)\b");
        var containsMarketLanguage = Regex.IsMatch(text, @"\b(MKT|MARKET|PLS|PLEASE|WHERE|SHOW|PRICE)\b");

        return (containsPair && containsTenor)
            || (containsPair && containsStructure)
            || (containsPair && containsMarketLanguage);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects clarification or follow-up messages.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <returns><c>true</c> if the text resembles a clarification; otherwise <c>false</c>.</returns>
    private static bool LooksLikeClarification(string text)
    {
        return Regex.IsMatch(text, @"\b(WHAT ABOUT|HOW ABOUT|SAME|BETTER|WIDER|TIGHTER|FLAT)\b");
    }
}