using System.Text.RegularExpressions;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Infrastructure.Normalization;

/// <summary>
/// Provides deterministic normalization for raw broker messages.
/// </summary>
public sealed class BrokerMessageNormalizer : IBrokerMessageNormalizer
{
    /// <summary>
    /// Normalizes a raw broker message into a structured normalized message.
    /// </summary>
    /// <param name="message">The raw message to normalize.</param>
    /// <returns>A normalized broker message.</returns>
    public NormalizedBrokerMessage Normalize(RawBrokerMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var normalizedText = message.RawText ?? string.Empty;
        normalizedText = normalizedText.Trim();
        normalizedText = normalizedText.ToUpperInvariant();
        normalizedText = NormalizeWhitespace(normalizedText);
        normalizedText = NormalizeCurrencyPair(normalizedText);
        normalizedText = NormalizeDeltaTokens(normalizedText);
        normalizedText = NormalizeStructureAliases(normalizedText);

        var tokens = SplitTokens(normalizedText);

        return new NormalizedBrokerMessage
        {
            RawMessage = message,
            NormalizedText = normalizedText,
            Tokens = tokens
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Collapses repeated whitespace and normalizes line breaks to single spaces.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <returns>The normalized text.</returns>
    private static string NormalizeWhitespace(string input)
    {
        return Regex.Replace(input, @"\s+", " ").Trim();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes common currency pair spellings into a six-letter format.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <returns>The updated text.</returns>
    private static string NormalizeCurrencyPair(string input)
    {
        var output = input;

        output = Regex.Replace(output, @"\b([A-Z]{3})\s*/\s*([A-Z]{3})\b", "$1$2");
        output = Regex.Replace(output, @"\b([A-Z]{3})\s+([A-Z]{3})\b", "$1$2");

        return output;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes common delta expressions into a compact D-suffixed format.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <returns>The updated text.</returns>
    private static string NormalizeDeltaTokens(string input)
    {
        var output = input;

        output = Regex.Replace(output, @"\b(\d{1,2})\s*DELTA\b", "$1D");
        output = Regex.Replace(output, @"\b(\d{1,2})\s*D\b", "$1D");

        return output;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes structure aliases such as RISK REVERSAL and FLY.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <returns>The updated text.</returns>
    private static string NormalizeStructureAliases(string input)
    {
        var output = input;

        output = Regex.Replace(output, @"\bRISK\s+REVERSAL\b", "RR");
        output = Regex.Replace(output, @"\bRISK\s+REV\b", "RR");
        output = Regex.Replace(output, @"\bBUTTERFLY\b", "BF");
        output = Regex.Replace(output, @"\bFLY\b", "BF");

        return output;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Splits normalized text into tokens using spaces.
    /// </summary>
    /// <param name="input">The normalized text.</param>
    /// <returns>A read-only token list.</returns>
    private static IReadOnlyList<string> SplitTokens(string input)
    {
        return input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}